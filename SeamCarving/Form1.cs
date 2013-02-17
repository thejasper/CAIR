using System;
using System.Drawing;
using System.Windows.Forms;

namespace SeamCarving
{
    public delegate void Update(int val);

    public partial class Form1 : Form
    {
        private CAIR cair;

        public Form1()
        {
            InitializeComponent();

            splitContainer1.IsSplitterFixed = true;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cair = new CAIR(new Bitmap(openFileDialog1.FileName));

                originalImageToolStripMenuItem.Checked = true;
                energyImageToolStripMenuItem.Checked = false;
                accEnergyImageToolStripMenuItem.Checked = false;

                saveToolStripMenuItem1.Enabled = true;
                showToolStripMenuItem.Enabled = true;

                btnResize.Enabled = true;

                splitContainer1.Panel2.BackgroundImage = cair.GetOriginalImage();
                lblSize.Text = cair.GetFormattedSize();
                nudNewWidth.Value = cair.width;
            }
        }

        private void originalImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            originalImageToolStripMenuItem.Checked = true;
            energyImageToolStripMenuItem.Checked = false;
            accEnergyImageToolStripMenuItem.Checked = false;

            splitContainer1.Panel2.BackgroundImage = cair.GetOriginalImage();
        }

        private void energyImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            originalImageToolStripMenuItem.Checked = false;
            energyImageToolStripMenuItem.Checked = true;
            accEnergyImageToolStripMenuItem.Checked = false;

            splitContainer1.Panel2.BackgroundImage = cair.GetEnergyImage();
        }

        private void accEnergyImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            originalImageToolStripMenuItem.Checked = false;
            energyImageToolStripMenuItem.Checked = false;
            accEnergyImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = cair.GetAccEnergyImage();
        }

        private void btnResize_Click(object sender, EventArgs e)
        {
            cair.Resize((val) => toolStripProgressBar1.Value = val, (int)nudNewWidth.Value);
            splitContainer1.Panel2.BackgroundImage = cair.GetOriginalImage();
        }
    }
}
