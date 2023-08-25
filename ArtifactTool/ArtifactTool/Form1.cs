using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArtifactTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // 装载副词条名称到下拉列表
            foreach(var item in Guesser.gSubStats)
                cb_SubStat.Items.Add(item.Key);

        }

        private void btn_StartGuess_Click(object sender, EventArgs e)
        {
            double subStatValue;
            bool isNum = double.TryParse(tb_Number.Text, out subStatValue);
            if (isNum == false)
            {
                MessageBox.Show("词条数值输入错误");
                return;
            }

            Guesser guesser = new Guesser(cb_SubStat.Text, subStatValue);

            // 是否需要将每个组合排序
            if(checkBox1.Checked == false)
                guesser.ScreenSingleOrders();

            // 额外信息显示到文本框
            tb_Info.Text += string.Format("升级次数：{0} \r\n", string.Join(",", guesser.UpgradeTimes()));
            tb_Info.Text += string.Format("词条数值档次：{0} \r\n", string.Join(",", guesser.GetGradeValues()));

            List<double[]> guessList = guesser.GuessList;
            // 调整表格行数
            dgv_GuessTable.Rows.Clear();
            int tableCount = dgv_GuessTable.Rows.Count;
            if(guessList.Count > tableCount)
                dgv_GuessTable.Rows.Add(guessList.Count - tableCount);

            // 猜测结果显示到表格
            for (int r = 0; r<guessList.Count; r++)
            {
                double[] d = guessList[r];

                double error = Math.Abs(d.Sum() - subStatValue);
                dgv_GuessTable[0, r].Value = error<0.001?0:error;

                for (int c = 0; c < d.Length; c++)
                {
                    dgv_GuessTable[c + 1, r].Value = d[c];
                }
            }

        }

        // 文本框增加新行后自动滑动至底部
        private void tb_Info_TextChanged(object sender, EventArgs e)
        {
            tb_Info.SelectionStart = tb_Info.Text.Length;
            tb_Info.ScrollToCaret();
        }

        // 增大下拉列表Item的间隔
        private void cb_SubStat_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            e.DrawBackground();
            e.DrawFocusRectangle();
            e.Graphics.DrawString(cb_SubStat.Items[e.Index].ToString(), e.Font,
                new SolidBrush(e.ForeColor), e.Bounds.X, e.Bounds.Y + 3);
        }
    }
}
