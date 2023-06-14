using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace background
{
    public partial class LayerList : Form
    {
        //public LayerList()
        //{
        //    InitializeComponent();
        //}
        public LayerList(string title, IEnumerable<string> items)
        {
            InitializeComponent();
            Text = title;
            comboBox1.DataSource = items.ToList();
            btn2.DialogResult = DialogResult.Cancel;
            btn1.DialogResult = DialogResult.OK;
        }

        public string SelectedItem => comboBox1.SelectedItem.ToString();

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
