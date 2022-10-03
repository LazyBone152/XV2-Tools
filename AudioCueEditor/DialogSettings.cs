using MahApps.Metro.Controls.Dialogs;

public static class DialogSettings
{
    public static MetroDialogSettings Default
    {
        get
        {
            return new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                DialogTitleFontSize = 16,
                DialogMessageFontSize = 12
            };
        }
    }

    public static MetroDialogSettings DefaultYesNo
    {
        get
        {
            return new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                DialogTitleFontSize = 16,
                DialogMessageFontSize = 12,
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No"
            };
        }
    }

    public static MetroDialogSettings ScrollDialog
    {
        get
        {
            return new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                DialogTitleFontSize = 16,
                DialogMessageFontSize = 12,
                MaximumBodyHeight = 300
            };
        }
    }

    public static MetroDialogSettings Create(int titleFontSize = 16, int messageFontSize = 12, string yesText = null, string noText = null)
    {
        MetroDialogSettings settings = Default;
        settings.DialogTitleFontSize = titleFontSize;
        settings.DialogMessageFontSize = messageFontSize;

        if (!string.IsNullOrWhiteSpace(yesText))
            settings.AffirmativeButtonText = yesText;

        if (!string.IsNullOrWhiteSpace(noText))
            settings.AffirmativeButtonText = noText;

        return settings;
    }
}