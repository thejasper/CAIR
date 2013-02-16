using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeamCarving
{
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

            splitContainer1.Panel2.BackgroundImage = cair.CalculateEnergy(cair.GetOriginalImage());
        }

        private void accEnergyImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            originalImageToolStripMenuItem.Checked = false;
            energyImageToolStripMenuItem.Checked = false;
            accEnergyImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = cair.CalculateAccumulatedEnergy(cair.GetOriginalImage());
        }

        private void btnResize_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2.BackgroundImage = cair.Resize(cair.GetOriginalImage(), (int)nudNewWidth.Value);
            
        }
    }
}
