using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;

namespace LB_Common.Forms
{
    public enum MessagePromptResult
    {
        OK = 0,
        Cancel = 1,
        Yes = 2,
        No = 3,
        PromptClosed = 4, //Form closed without the user pressing a button

        //Generic ones for custom button names
        Affirmative = 0,
        Negative = 1,
        AffirmativeAlt = 2,
        NegativeAlt = 3
    }

    [Flags]
    public enum MessagePromptButtons
    {
        OK = 1,
        Cancel = 2,
        Yes = 4,
        No = 8,

        //Combinations
        OKCancel = OK | Cancel,
        YesNo = Yes | No,
        YesNoCancel = Yes | No | Cancel,

        //Generic ones for custom button names
        Affirmative = 1,
        Negative = 2,
        AffirmativeAlt = 4,
        NegativeAlt = 8,

        //Generic combinations
        AffirmativeNegative = Affirmative | Negative,
        All = Affirmative | Negative | AffirmativeAlt | NegativeAlt
    }

    public enum MessagePromptIcon
    {
        None = 0,
        Information = 1,
        Question = 2,
        Error = 3,
        Stop = 3,
        Warning = 4

    }

    public partial class MessagePrompt : MetroWindow
    {
        public MessagePromptResult Result = MessagePromptResult.PromptClosed;

        public MessagePrompt(string title, string message, string richText = null) 
            : this(title, message, MessagePromptIcon.Information, MessagePromptButtons.OK, richText, null, null, null, null, topmost: false)
        {

        }

        public MessagePrompt(string title, string message, MessagePromptIcon icon, MessagePromptButtons buttons, string richText = null,
            string affirmativeBtnAlias = null, string affirmativeAltBtnAlias = null, string negativeBtnAlias = null, string negativeAltBtnAlias = null, bool topmost = false)
        {
            InitializeComponent();
            DataContext = this;

            Topmost = topmost;
            Title = title;
            helpTextBlock.Text = message;
            richTextBox.Visibility = string.IsNullOrWhiteSpace(richText) ? Visibility.Collapsed : Visibility.Visible;

            if(richText != null)
                richTextBox.AppendText(richText);

            if (!buttons.HasFlag(MessagePromptButtons.OK))
                okBtn.Visibility = Visibility.Collapsed;
            if (!buttons.HasFlag(MessagePromptButtons.Cancel))
                cancelBtn.Visibility = Visibility.Collapsed;
            if (!buttons.HasFlag(MessagePromptButtons.Yes))
                yesBtn.Visibility = Visibility.Collapsed;
            if (!buttons.HasFlag(MessagePromptButtons.No))
                noBtn.Visibility = Visibility.Collapsed;

            //Set custom names for buttons
            if(!string.IsNullOrWhiteSpace(affirmativeBtnAlias))
                okBtn.Content = affirmativeBtnAlias;
            if (!string.IsNullOrWhiteSpace(affirmativeAltBtnAlias))
                yesBtn.Content = affirmativeBtnAlias;
            if (!string.IsNullOrWhiteSpace(negativeBtnAlias))
                cancelBtn.Content = affirmativeBtnAlias;
            if (!string.IsNullOrWhiteSpace(negativeAltBtnAlias))
                noBtn.Content = affirmativeBtnAlias;

            //Set icon
            switch (icon)
            {
                case MessagePromptIcon.None:
                    this.icon.Visibility = Visibility.Collapsed;
                    break;
                case MessagePromptIcon.Information:
                    this.icon.Kind = PackIconMaterialLightKind.Information;
                    this.icon.Foreground = Brushes.DeepSkyBlue;
                    break;
                case MessagePromptIcon.Question:
                    this.icon.Kind = PackIconMaterialLightKind.HelpCircle;
                    this.icon.Foreground = Brushes.DeepSkyBlue;
                    break;
                case MessagePromptIcon.Stop:
                    this.icon.Kind = PackIconMaterialLightKind.AlertCircle;
                    this.icon.Foreground = Brushes.Red;
                    break;
                case MessagePromptIcon.Warning:
                    this.icon.Kind = PackIconMaterialLightKind.AlertCircle;
                    this.icon.Foreground = Brushes.OrangeRed;
                    break;
            }

        }

        public static MessagePromptResult Show(string message, string title, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, MessagePromptIcon.Information, MessagePromptButtons.OK, null, noSound: noSound, topmost: topmost);
        }

        public static MessagePromptResult Show(string message, string title, string richText, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, MessagePromptIcon.Information, MessagePromptButtons.OK, richText, noSound: noSound, topmost: topmost);
        }

        public static MessagePromptResult Show(string message, string title, MessagePromptIcon icon, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, icon, MessagePromptButtons.OK, null, noSound: noSound, topmost: topmost);
        }

        public static MessagePromptResult Show(string message, string title, MessagePromptButtons buttons, MessagePromptIcon icon, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, icon, buttons, null, noSound: noSound, topmost: topmost);
        }

        public static MessagePromptResult Show(string message, string title, MessagePromptButtons buttons, MessagePromptIcon icon, string richText, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, icon, buttons, richText, noSound: noSound, topmost: topmost);
        }

        public static MessagePromptResult Show(string message, string title, MessagePromptButtons buttons, MessagePromptIcon icon, string richText, string affirmativeBtnAlias, string affirmativeAltBtnAlias, string negativeBtnAlias, string negativeAltBtnAlias, bool noSound = false, bool topmost = true)
        {
            return InternalShow(title, message, icon, buttons, richText, affirmativeBtnAlias, affirmativeAltBtnAlias, negativeBtnAlias, negativeAltBtnAlias, noSound: noSound, topmost: topmost);
        }

        private static MessagePromptResult InternalShow(string title, string message, MessagePromptIcon icon, MessagePromptButtons buttons, string richText = null,
            string affirmativeBtnAlias = null, string affirmativeAltBtnAlias = null, string negativeBtnAlias = null, string negativeAltBtnAlias = null, bool noSound = false, bool topmost = true)
        {
            MessagePrompt prompt = new MessagePrompt(title, message, icon, buttons, richText, affirmativeBtnAlias, affirmativeAltBtnAlias, negativeBtnAlias, negativeAltBtnAlias, topmost);

            if (!noSound)
            {
                switch (icon)
                {
                    case MessagePromptIcon.Information:
                        SystemSounds.Asterisk.Play();
                        break;
                    case MessagePromptIcon.Question:
                        SystemSounds.Question.Play();
                        break;
                    case MessagePromptIcon.Warning:
                        SystemSounds.Exclamation.Play();
                        break;
                    case MessagePromptIcon.Error:
                        SystemSounds.Hand.Play();
                        break;
                }
            }

            prompt.ShowDialog();
            return prompt.Result;
        }

        #region Events
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Result = MessagePromptResult.OK;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessagePromptResult.Cancel;
            Close();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessagePromptResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessagePromptResult.No;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                Result = MessagePromptResult.Affirmative;
                e.Handled = true;
                Close();
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                Result = MessagePromptResult.Negative;
                e.Handled = true;
                Close();
            }
        }

        #endregion
    }
}