using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TextBox = System.Windows.Controls.TextBox;

namespace Re_Albumizer
{
    /// <summary>
    /// Interaction logic for EditObject.xaml
    /// </summary>
    public partial class EditObject : Window
    {
        private readonly InputMode _currentInputMode;
        public EditObject()
        {
            InitializeComponent();
        }
        
        
        public EditObject(InputMode i)
        {
            InitializeComponent();
            
            _currentInputMode = i;
            switch (i)
            {
                case InputMode.ARRAYTEXT:
                    ArrayTextInput.Visibility=Visibility.Visible;
                    TextBoxInput.Visibility = Visibility.Collapsed;
                    break;
                case InputMode.TEXT:
                    TextBoxInput.Visibility=Visibility.Visible;
                    ArrayTextInput.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        public enum InputMode : uint
        {
            TEXT = 0,
            
            ARRAYTEXT =2,
        }

        private void AddNew_OnClick(object sender, RoutedEventArgs e)
        {
            TextBox tb = new TextBox();
            tb.ToolTip = "Type Something!";
            ListStacker.Children.Add(tb);
            tb.Focus();
        }

        public dynamic ShowForm()
        {

            this.ShowDialog();
            
            switch (_currentInputMode)
            {
                
                case InputMode.ARRAYTEXT:
                    List<string> returnList = new List<string>();
                    foreach (var child in ListStacker.Children.OfType<TextBox>())
                    {
                        returnList.Add(child.Text);
                    }
                    return returnList.ToArray();
                    
                case InputMode.TEXT:
                    return TextBoxInput.Text;
                    
            }

            return "";
        }

        private void Okbutton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
