using System;
using System.Text;
using System.Windows.Forms;

namespace AStyle4VS
{
    public partial class AStyleOptionControl : UserControl
    {
        private enum AStyleOptionControlType
        {
            Simple,
            Selectable,
            Numeric,
            SelectableAndNumeric
        }
        private readonly AStyleOptionControlType type;

        private bool invokeChangeEvent = true;
        public event EventHandler Changed;

        public AStyleOptionControl(string name, string description)
        {
            InitializeComponent();

            type = AStyleOptionControlType.Simple;

            checkBox.Text = name;

            toolTip.SetToolTip(checkBox, description);
        }

        public AStyleOptionControl(string name, string description, string[] stringValues) : this(name, description)
        {
            type = AStyleOptionControlType.Selectable;

            comboBox.Visible = true;
            comboBox.Items.AddRange(stringValues);

            toolTip.SetToolTip(comboBox, description);
        }

        public AStyleOptionControl(string name, string description, int minNumValue, int maxNumValue) : this(name, description)
        {
            type = AStyleOptionControlType.Numeric;

            numericUpDown.Visible = true;
            numericUpDown.Minimum = minNumValue;
            numericUpDown.Maximum = maxNumValue;

            toolTip.SetToolTip(numericUpDown, description);
        }

        public AStyleOptionControl(string name, string description, string[] stringValues, int minNumValue, int maxNumValue) : this(name, description, stringValues)
        {
            type = AStyleOptionControlType.SelectableAndNumeric;

            checkBox1.Visible = true;

            numericUpDown.Visible = true;
            numericUpDown.Minimum = minNumValue;
            numericUpDown.Maximum = maxNumValue;

            toolTip.SetToolTip(checkBox1, description);
            toolTip.SetToolTip(numericUpDown, description);
        }

        public void Reset()
        {
            invokeChangeEvent = false;

            checkBox.Checked = false;

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }

            checkBox1.Checked = false;

            numericUpDown.Value = numericUpDown.Minimum;

            invokeChangeEvent = true;
        }

        public void ParseCommandLineArgument(string option)
        {
            invokeChangeEvent = false;

            string[] values = option.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            string name = checkBox.Text.TrimEnd('=');
            switch (values.Length)
            {
            case 1:
                switch (type)
                {
                case AStyleOptionControlType.Simple:
                    if (values[0] == name)
                    {
                        checkBox.Checked = true;
                    }
                    break;
                }
                break;

            case 2:
                switch (type)
                {
                case AStyleOptionControlType.Selectable:
                case AStyleOptionControlType.SelectableAndNumeric:
                    if (values[0] == name)
                    {
                        foreach (string item in comboBox.Items)
                        {
                            if (values[1] == item)
                            {
                                checkBox.Checked = true;
                                comboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    break;

                case AStyleOptionControlType.Numeric:
                    if (values[0] == name)
                    {
                        if (int.TryParse(values[1], out int value) && value >= numericUpDown.Minimum && value <= numericUpDown.Maximum)
                        {
                            checkBox.Checked = true;
                            numericUpDown.Value = value;
                        }
                    }
                    break;
                }
                break;

            case 3:
                switch (type)
                {
                case AStyleOptionControlType.SelectableAndNumeric:
                    if (values[0] == name)
                    {
                        foreach (string item in comboBox.Items)
                        {
                            if (values[1] == item)
                            {
                                if (int.TryParse(values[2], out int value) && value >= numericUpDown.Minimum && value <= numericUpDown.Maximum)
                                {
                                    checkBox.Checked = true;
                                    comboBox.SelectedItem = item;
                                    checkBox1.Checked = true;
                                    numericUpDown.Value = value;
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
                break;
            }

            invokeChangeEvent = true;
        }

        public string GetCommandLineArgument()
        {
            StringBuilder commandLineArgument = new StringBuilder();
            if (checkBox.Checked)
            {
                commandLineArgument.Append(checkBox.Text);
                switch (type)
                {
                case AStyleOptionControlType.Selectable:
                case AStyleOptionControlType.SelectableAndNumeric:
                    commandLineArgument.Append(comboBox.Text);
                    if (checkBox1.Checked)
                    {
                        commandLineArgument.Append(checkBox1.Text);
                        goto case AStyleOptionControlType.Numeric;
                    }
                    break;

                case AStyleOptionControlType.Numeric:
                    commandLineArgument.Append(numericUpDown.Value.ToString());
                    break;
                }
            }

            return commandLineArgument.ToString();
        }

        private void OnChanged(object sender, EventArgs e)
        {
            switch (type)
            {
            case AStyleOptionControlType.Selectable:
                comboBox.Enabled = checkBox.Checked;
                break;

            case AStyleOptionControlType.Numeric:
                numericUpDown.Enabled = checkBox.Checked;
                break;

            case AStyleOptionControlType.SelectableAndNumeric:
                checkBox1.Enabled = comboBox.Enabled = checkBox.Checked;
                numericUpDown.Enabled = checkBox1.Checked;
                break;
            }

            if (invokeChangeEvent)
            {
                Changed?.Invoke(this, e);
            }
        }
    }
}
