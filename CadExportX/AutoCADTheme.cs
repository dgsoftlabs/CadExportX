using System.Drawing;

namespace ModelSpace
{
    /// <summary>
    /// AutoCAD-style dark theme color scheme
    /// </summary>
    public static class AutoCADTheme
    {
        // Background Colors
        public static readonly Color DarkBackground = Color.FromArgb(44, 44, 44);

        public static readonly Color DarkerBackground = Color.FromArgb(30, 30, 30);
        public static readonly Color LightBackground = Color.FromArgb(63, 63, 63);

        // Accent Colors
        public static readonly Color Accent = Color.FromArgb(0, 122, 204);

        public static readonly Color AccentHover = Color.FromArgb(30, 138, 214);
        public static readonly Color AccentPressed = Color.FromArgb(0, 102, 184);

        // Text Colors
        public static readonly Color TextPrimary = Color.FromArgb(241, 241, 241);

        public static readonly Color TextSecondary = Color.FromArgb(176, 176, 176);
        public static readonly Color TextDisabled = Color.FromArgb(128, 128, 128);

        // UI Element Colors
        public static readonly Color Border = Color.FromArgb(85, 85, 85);

        public static readonly Color Separator = Color.FromArgb(60, 60, 60);

        // Status Colors
        public static readonly Color Success = Color.FromArgb(0, 200, 81);

        public static readonly Color Warning = Color.FromArgb(255, 140, 0);
        public static readonly Color Error = Color.FromArgb(232, 17, 35);
        public static readonly Color Info = Color.FromArgb(0, 120, 215);

        /// <summary>
        /// Apply dark theme to a Windows Forms control
        /// </summary>
        public static void ApplyTo(System.Windows.Forms.Control control)
        {
            control.BackColor = DarkBackground;
            control.ForeColor = TextPrimary;

            if (control is System.Windows.Forms.Button button)
            {
                button.BackColor = LightBackground;
                button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = Accent;
            }
            else if (control is System.Windows.Forms.TextBox textBox)
            {
                textBox.BackColor = DarkerBackground;
                textBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            }
            else if (control is System.Windows.Forms.TreeView treeView)
            {
                treeView.BackColor = DarkerBackground;
                treeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                treeView.LineColor = Border;
            }
            else if (control is System.Windows.Forms.PropertyGrid propertyGrid)
            {
                propertyGrid.BackColor = DarkerBackground;
                propertyGrid.ViewBackColor = DarkerBackground;
                propertyGrid.ViewForeColor = TextPrimary;
                propertyGrid.CategoryForeColor = Accent;
                propertyGrid.CategorySplitterColor = Border;
                propertyGrid.HelpBackColor = DarkBackground;
                propertyGrid.HelpForeColor = TextSecondary;
                propertyGrid.LineColor = Border;
            }

            // Recursively apply to child controls
            if (control.HasChildren)
            {
                foreach (System.Windows.Forms.Control childControl in control.Controls)
                {
                    ApplyTo(childControl);
                }
            }
        }
    }
}