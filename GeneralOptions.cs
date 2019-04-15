using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace AStyle4VS
{
    public partial class GeneralOptionsControl : UserControl
    {
        private readonly GeneralOptionsPage generalOptionsPage;

        public GeneralOptionsControl(GeneralOptionsPage generalOptionsPage)
        {
            InitializeComponent();

            this.generalOptionsPage = generalOptionsPage;

            textBox2.Enabled = checkBox1.Checked = generalOptionsPage.FormatOnSave;
            textBox2.Lines = generalOptionsPage.ExcludeedExtensions;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            generalOptionsPage.FormatOnSave = textBox2.Enabled = checkBox1.Checked;
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            generalOptionsPage.ExcludeedExtensions = textBox2.Lines;
        }
    }

    [Guid("13D951A6-4709-4019-BCAE-FF2F9BCF6986")]
    public class GeneralOptionsPage : DialogPage
    {
        public virtual bool FormatOnSave { get; set; } = false;
        public virtual string[] ExcludeedExtensions { get; set; } = new string[]
        {
            "*.Designer.cs",
            "*.rc",
            "*.rc2"
        };

        protected override IWin32Window Window
        {
            get
            {
                return new GeneralOptionsControl(this);
            }
        }
    }
}
