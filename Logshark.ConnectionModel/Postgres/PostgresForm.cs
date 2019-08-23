using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Logshark.ConnectionModel.Postgres
{
    public partial class PostgresForm : Form
    {
        public PostgresForm()
        {
            InitializeComponent();
        }
        private void PostgresForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Postgres.PostgresConnectionInfo.PostgresFormUser = textBox4.Text;
            Postgres.PostgresConnectionInfo.PostgresFormUser = textBox3.Text;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
