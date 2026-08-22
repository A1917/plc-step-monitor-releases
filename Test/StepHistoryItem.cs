using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    public partial class StepHistoryItem : UserControl
    {

        public StepHistoryItem()
        {
            InitializeComponent();
            
        }

        public int StepNum { get; set; }
        public string StepDesc { get; set; }
        public string StepDuration { get; set; }

        public event Action<int, string> OnDescSaved;

        public void BtnSave(object sender , EventArgs e)
        {
            string newDesc = tb_Desc.Text.Trim();
            OnDescSaved?.Invoke(StepNum, newDesc);
        }
        
    }
}
