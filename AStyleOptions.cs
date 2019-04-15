using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AStyle4VS
{
    public partial class AStyleOptionsControl : UserControl
    {
        private readonly AStyleOptionsPage astyleOptionsPage;

        private readonly AStyleOptionControl[] braceStyleOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--style=", "Brace style to use.", new string[]
            {
                "allman",
                "bsd",
                "break",
                "java",
                "attach",
                "kr",
                "k&r",
                "k/r",
                "stroustrup",
                "whitesmith",
                "vtk",
                "ratliff",
                "banner",
                "gnu",
                "linux",
                "knf",
                "horstmann",
                "run-in",
                "1tbs",
                "otbs",
                "google",
                "mozilla",
                "pico",
                "lisp",
                "python",
            })
        };
        private readonly AStyleOptionControl[] tabOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--indent=", "", new string[]
            {
                "spaces",
                "tab",
                "force-tab",
                "force-tab-x",
            },
            2, 20)
        };
        private readonly AStyleOptionControl[] braceModifyOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--attach-namespaces",    "Attach braces to a namespace statement"),
            new AStyleOptionControl("--attach-classes",       "Attach braces to a class statement"),
            new AStyleOptionControl("--attach-inlines",       "Attach braces to class and struct inline function definitions"),
            new AStyleOptionControl("--attach-extern-c",      "Attach braces to a braced extern \"C\" statement"),
            new AStyleOptionControl("--attach-closing-while", "Attach braces to a namespace statement")
        };
        private readonly AStyleOptionControl[] indentationOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--indent-classes",          "Indent 'class' and 'struct' blocks so that the entire block is indented"),
            new AStyleOptionControl("--indent-modifiers",        "Indent 'class ' and 'struct' access modifiers, 'public:', 'protected:' and 'private:', one half indent"),
            new AStyleOptionControl("--indent-switches",         "Indent 'switch' blocks so that the 'case X:' statements are indented in the switch block"),
            new AStyleOptionControl("--indent-cases",            "Indent 'case X:' blocks from the 'case X:' headers. Case statements not enclosed in blocks are NOT indented"),
            new AStyleOptionControl("--indent-namespaces",       "Add extra indentation to namespace blocks"),
            new AStyleOptionControl("--indent-after-parens",     ""), // TODO
            new AStyleOptionControl("--indent-continuation=",     "Indent 'class' and 'struct' blocks so that the entire block is indented", 0, 4),
            new AStyleOptionControl("--indent-labels",           "Add extra indentation to labels so they appear 1 indent less than the current indentation, rather than being flushed to the left (the default)"), // TODO
            new AStyleOptionControl("--indent-preproc-block",    "Indent preprocessor blocks at brace level zero and immediately within a namespace"),
            new AStyleOptionControl("--indent-preproc-define",   "Indent multi-line preprocessor definitions ending with a backslash"),
            new AStyleOptionControl("--indent-preproc-cond",     "Indent preprocessor conditional statements to the same level as the source code"),
            new AStyleOptionControl("--indent-col1-comments",    "Indent C++ comments beginning in column one"),
            new AStyleOptionControl("--min-conditional-indent=",  "Set the minimal indent that is added when a header is built of multiple lines", 0, 10),
            new AStyleOptionControl("--max-continuation-indent=", "Set the  maximum of # spaces to indent a continuation line", 0, 10)
        };
        private readonly AStyleOptionControl[] paddingOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--break-blocks",        "Pad empty lines around header blocks (e.g. 'if', 'for', 'while'...)."),
            new AStyleOptionControl("--break-blocks=all",    "Pad empty lines around header blocks (e.g. 'if', 'for', 'while'...). Treat closing header blocks (e.g. 'else', 'catch') as stand-alone blocks."),
            new AStyleOptionControl("--pad-oper",            "Insert space padding around operators. This will also pad commas. "),
            new AStyleOptionControl("--pad-comma",           "Attach braces to a braced extern \"C\" statement"),
            new AStyleOptionControl("--pad-paren",           "Attach braces to a namespace statement"),
            new AStyleOptionControl("--pad-paren-out",       "Attach braces to a namespace statement"),
            new AStyleOptionControl("--pad-first-paren-out", "Attach braces to a namespace statement"),
            new AStyleOptionControl("--pad-paren-in",        "Attach braces to a namespace statement"),
            new AStyleOptionControl("--pad-header",          "Attach braces to a namespace statement"),
            new AStyleOptionControl("--unpad-paren",         "Attach braces to a namespace statement"),
            new AStyleOptionControl("--delete-empty-lines",  "Attach braces to a namespace statement"),
            new AStyleOptionControl("--fill-empty-lines",    "Attach braces to a namespace statement"),
            new AStyleOptionControl("--align-pointer=",       "Attach a pointer or reference operator (*, &, or ^) to either the variable type (left) or variable name (right), or place it between the type and name (middle).", new string[]
            {
                "type",
                "middle",
                "name"
            }),
            new AStyleOptionControl("--align-reference=",     "This option will align references separate from pointers. Pointers are not changed by this option.", new string[]
            {
                "none",
                "type",
                "middle",
                "name"
            })
        };
        private readonly AStyleOptionControl[] formattingOptions = new AStyleOptionControl[]
        {
            new AStyleOptionControl("--break-closing-braces",     ""),
            new AStyleOptionControl("--break-elseifs",            ""),
            new AStyleOptionControl("--break-one-line-headers",   ""),
            new AStyleOptionControl("--add-braces",               ""),
            new AStyleOptionControl("--add-one-line-braces",      ""),
            new AStyleOptionControl("--remove-braces",            ""),
            new AStyleOptionControl("--break-return-type",        ""),
            new AStyleOptionControl("--break-return-type-decl",   ""),
            new AStyleOptionControl("--attach-return-type",       ""),
            new AStyleOptionControl("--attach-return-type-decl",  ""),
            new AStyleOptionControl("--keep-one-line-blocks",     ""),
            new AStyleOptionControl("--keep-one-line-statements", ""),
            new AStyleOptionControl("--convert-tabs",             ""),
            new AStyleOptionControl("--close-templates",          ""),
            new AStyleOptionControl("--remove-comment-prefix",    ""),
            new AStyleOptionControl("--max-code-length",          ""),
            new AStyleOptionControl("--break-after-logical",      "")
        };

        public AStyleOptionsControl(AStyleOptionsPage astyleOptionsPage)
        {
            InitializeComponent();

            void InitializeOptionPanel(TableLayoutPanel panel, AStyleOptionControl[] options)
            {
                panel.ColumnStyles.Clear();
                panel.RowStyles.Clear();
                foreach (AStyleOptionControl option in options)
                {
                    option.Changed += OnOptionChanged;

                    panel.Controls.Add(option);
                }
            }
            InitializeOptionPanel(tableLayoutPanel1, braceStyleOptions);
            InitializeOptionPanel(tableLayoutPanel2, tabOptions);
            InitializeOptionPanel(tableLayoutPanel3, braceModifyOptions);
            InitializeOptionPanel(tableLayoutPanel4, indentationOptions);
            InitializeOptionPanel(tableLayoutPanel5, paddingOptions);
            InitializeOptionPanel(tableLayoutPanel6, formattingOptions);

            this.astyleOptionsPage = astyleOptionsPage;

            commandLineTextBox.TextChanged -= CommandLineTextBoxChanged;
            commandLineTextBox.Text = astyleOptionsPage.CommandLine;
            commandLineTextBox.TextChanged += CommandLineTextBoxChanged;

            ParseCommandLine(commandLineTextBox.Text);
        }

        private void ParseCommandLine(string commandLine)
        {
            void ResetOptions(AStyleOptionControl[] options)
            {
                foreach (AStyleOptionControl option in options)
                {
                    option.Reset();
                }
            }
            ResetOptions(braceStyleOptions);
            ResetOptions(tabOptions);
            ResetOptions(braceModifyOptions);
            ResetOptions(indentationOptions);
            ResetOptions(paddingOptions);
            ResetOptions(formattingOptions);

            foreach (string commandLineArgument in commandLine.Split(new char[] { ' ', '\t', '\n', '\v', '\f', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                void ParseCommandLineArguments(AStyleOptionControl[] options)
                {
                    foreach (AStyleOptionControl option in options)
                    {
                        option.ParseCommandLineArgument(commandLineArgument);
                    }
                }
                ParseCommandLineArguments(braceStyleOptions);
                ParseCommandLineArguments(tabOptions);
                ParseCommandLineArguments(braceModifyOptions);
                ParseCommandLineArguments(indentationOptions);
                ParseCommandLineArguments(paddingOptions);
                ParseCommandLineArguments(formattingOptions);
            }
        }

        private void OnOptionChanged(object sender, EventArgs e)
        {
            StringBuilder commandLine = new StringBuilder();
            void GetOptions(AStyleOptionControl[] options)
            {
                foreach (AStyleOptionControl option in options)
                {
                    commandLine.Append(option.GetCommandLineArgument()).Append(' ');
                }
            }
            GetOptions(braceStyleOptions);
            GetOptions(tabOptions);
            GetOptions(braceModifyOptions);
            GetOptions(indentationOptions);
            GetOptions(paddingOptions);
            GetOptions(formattingOptions);

            commandLineTextBox.TextChanged -= CommandLineTextBoxChanged;
            commandLineTextBox.Text = commandLine.ToString().TrimEnd();
            commandLineTextBox.TextChanged += CommandLineTextBoxChanged;

            astyleOptionsPage.CommandLine = commandLineTextBox.Text;
        }

        private void CommandLineTextBoxChanged(object sender, EventArgs e)
        {
            ParseCommandLine(commandLineTextBox.Text);

            astyleOptionsPage.CommandLine = commandLineTextBox.Text;
        }
    }

    public class AStyleOptionsPage : DialogPage
    {
        protected virtual string DefaultCommandLine { get; } = string.Empty;
        private string commandLine = string.Empty;
        public virtual string CommandLine
        {
            get
            {
                return commandLine;
            }
            set
            {
                commandLine = value + DefaultCommandLine;
            }
        }

        protected override IWin32Window Window
        {
            get
            {
                return new AStyleOptionsControl(this);
            }
        }
    }

    [Guid("8CF4CC30-AC69-4874-AE24-41A3275F471D")]
    public class COptionsPage : AStyleOptionsPage
    {
    }

    [Guid("7E786F9F-AD49-4335-B632-5634FC7E8EB0")]
    public class CSharpOptionsPage : AStyleOptionsPage
    {
    }
}
