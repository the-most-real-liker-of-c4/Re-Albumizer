using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
 using MColor = System.Windows.Media.Color;
  using DColor = System.Drawing.Color;
namespace Re_Albumizer
{
   


 /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            
            MGrid.Background=new SolidColorBrush(DToMediaColor(System.Drawing.SystemColors.Control));
            ProgName.Content=Assembly.GetExecutingAssembly().GetName().Name;
            ProgVer.Content=Assembly.GetExecutingAssembly().GetName().Version;
            ProgCopyright.Content=((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute),false)[0]).Copyright;
            ProgCopyright.Content=((AssemblyCompanyAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute),false)[0]).Company;
            ProgDescript.Text=((AssemblyDescriptionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute),false)[0]).Description;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public static MColor DToMediaColor(DColor color)
        {
             return MColor.FromArgb(color.A, color.R, color.G, color.B);
        }
   
    }
}
