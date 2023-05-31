using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shon
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        string login = "admin";
        string password = "admin";

        bool access = false;
        public bool Access { get { return access; } }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == login && textBox2.Text == password) 
            { 
                access = true; 
                this.Close(); 
            }
            else 
            { 
                access = false; 
                MessageBox.Show("Невернен логин или пароль", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
            textBox1.Text = null;
            textBox2.Text = null;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) button1_Click(null, null);
        }
    }
}
