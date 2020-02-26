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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nokk.Controls
{
    public partial class SearchResultItem : UserControl
    {
        public BitmapImage image { get; set; }
        public string header { get; set; }
        public string description { get; set; }

        public SearchResultItem()
        {
            InitializeComponent();
        }
    }
}
