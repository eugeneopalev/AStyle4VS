using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

// TODO: http://astyle.sourceforge.net/develop/sharp.html
namespace AStyle4VS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#1000", "#1001", "1.0", IconResourceID = 400)]
    [Guid("DCED2B69-BA3C-410E-AEC7-BFDDDB170CDD")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "AStyle4VS", "General", 1000, 1010, false)]
    [ProvideProfile(typeof(GeneralOptionsPage), "AStyle4VS", "General", 1000, 1010, true)]
    [ProvideOptionPage(typeof(COptionsPage), "AStyle4VS", "C/C++", 1000, 1011, true)]
    [ProvideProfile(typeof(COptionsPage), "AStyle4VS", "C/C++", 1000, 1011, true)]
    [ProvideOptionPage(typeof(CSharpOptionsPage), "AStyle4VS", "C#", 1000, 1012, true)]
    [ProvideProfile(typeof(CSharpOptionsPage), "AStyle4VS", "C#", 1000, 1012, true)]
    public sealed class AStyle4VS : AsyncPackage, IVsRunningDocTableEvents3
    {
        private delegate void fpError(int errorNumber, [MarshalAs(UnmanagedType.LPStr)] string errorMessage);
        private delegate IntPtr fpAlloc(int memoryNeeded);

        [DllImport("AStyle31", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr AStyleMainUtf16([MarshalAs(UnmanagedType.LPWStr)] string pSourceIn, [MarshalAs(UnmanagedType.LPWStr)] string pOptions, fpError fpErrorHandler, fpAlloc fpMemoryAlloc);

        private DTE dte;
        private IVsRunningDocumentTable rdt;

        GeneralOptionsPage generalOptionsPage;
        AStyleOptionsPage cppOptionsPage, csOptionsPage;

        private uint cookie;
        private bool disposed;

        #region AsyncPackage Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            dte = await GetServiceAsync(typeof(DTE)) as DTE;
            rdt = GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            rdt.AdviseRunningDocTableEvents(this, out cookie);

            OleMenuCommandService commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            //Assumes.Present(commandService);

            OleMenuCommand command = new OleMenuCommand(FormatDocumentCall, null, OnBeforeQueryStatus, new CommandID(new Guid("497A6C02-150A-4169-A09B-8592571306B9"), 0x0100));
            commandService.AddCommand(command);

            generalOptionsPage = GetDialogPage(typeof(GeneralOptionsPage)) as GeneralOptionsPage;
            csOptionsPage = GetDialogPage(typeof(CSharpOptionsPage)) as AStyleOptionsPage;
            cppOptionsPage = GetDialogPage(typeof(COptionsPage)) as AStyleOptionsPage;
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (disposed)
            {
                return;
            }

            disposed = true;

            if (rdt != null)
            {
                rdt.UnadviseRunningDocTableEvents(cookie);
                rdt = null;
            }
        }

        #endregion

        #region IVsRunningDocTableEvents3 Members

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // S_OK on success, error code on error
            rdt.GetDocumentInfo(docCookie, out uint flags, out uint readlocks, out uint editlocks, out string name, out IVsHierarchy hier, out uint itemid, out IntPtr docData);

            Document document = dte.Documents.OfType<Document>().FirstOrDefault(x => x.FullName == name);
            FormatDocument(document, true);

            return VSConstants.S_OK;
        }

        #endregion

        private void FormatDocumentCall(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            FormatDocument(dte.ActiveDocument);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ((OleMenuCommand)sender).Visible = (dte.ActiveDocument != null && (dte.ActiveDocument.Language == "C/C++" || dte.ActiveDocument.Language == "CSharp"));
        }

        private void FormatDocument(Document doc, bool beforeSave = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool FilenameMatchesPattern(string filename, string pattern)
            {
                // TODO: Need to use some regular expression here

                return false;
            }
            if (beforeSave && generalOptionsPage.FormatOnSave)
            {
                foreach (string extension in generalOptionsPage.ExcludeedExtensions)
                {
                    if (FilenameMatchesPattern(Path.GetFileName(doc.FullName), extension))
                    {
                        return;
                    }
                }
            }

            void FormatDocument(string options)
            {
                if (doc.Object("TextDocument") is TextDocument textDocument)
                {
                    EditPoint startEditPoint = textDocument.StartPoint.CreateEditPoint();
                    EditPoint endEditPoint = textDocument.EndPoint.CreateEditPoint();
                    IntPtr unmanagedText = AStyleMainUtf16(startEditPoint.GetText(endEditPoint), options, OnAStyleError, Marshal.AllocHGlobal);
                    if (unmanagedText != IntPtr.Zero)
                    {
                        string text = Marshal.PtrToStringUni(unmanagedText);
                        if (!string.IsNullOrEmpty(text))
                        {
                            startEditPoint.ReplaceText(endEditPoint, text, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
                        }
                        Marshal.FreeHGlobal(unmanagedText);
                    }
                }
            }
            switch (doc.Language)
            {
            case "C/C++":
                FormatDocument(cppOptionsPage.CommandLine);
                break;
            case "CSharp":
                FormatDocument(csOptionsPage.CommandLine);
                break;
            }
        }

        private static void OnAStyleError(int errorNumber, string errorMessage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            // TODO: Show some message box here
        }
    }
}
