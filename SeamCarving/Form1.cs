using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SeamCarving
{
    public delegate void Update(int val);

    public partial class Form1 : Form
    {
        private CAIR cair;
        private Bitmap toSave;

        public Form1()
        {
            InitializeComponent();

            splitContainer1.IsSplitterFixed = true;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
        }

        private void DefaultLayout()
        {
            UncheckEverything();
            originalImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = toSave = cair.GetOriginalImage();
        }

        private void UncheckEverything()
        {
            originalImageToolStripMenuItem.Checked = false;
            energyImageToolStripMenuItem.Checked = false;
            accEnergyImageToolStripMenuItem.Checked = false;
            seamsToolStripMenuItem.Checked = false;
        }

        private void originalImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckEverything();
            originalImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = toSave = cair.GetOriginalImage();
        }

        private void energyImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckEverything();
            energyImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = toSave = cair.GetEnergyImage();
        }

        private void accEnergyImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckEverything();
            accEnergyImageToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = toSave = cair.GetAccEnergyImage();
        }

        private void seamsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckEverything();
            seamsToolStripMenuItem.Checked = true;

            splitContainer1.Panel2.BackgroundImage = toSave = cair.GetSeamImage();
        }

        private void btnResize_Click(object sender, EventArgs e)
        {
            cair.withForwardEnergy = cbForwardEnergy.Checked;
            cair.Resize(val => toolStripProgressBar1.Value = val, (int)nudNewWidth.Value);

            DefaultLayout();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cair = new CAIR(new Bitmap(openFileDialog1.FileName));

                DefaultLayout();

                saveToolStripMenuItem1.Enabled = true;
                showToolStripMenuItem.Enabled = true;
                btnResize.Enabled = true;

                lblSize.Text = cair.GetFormattedSize();
                nudNewWidth.Value = cair.width;
                nudNewHeight.Value = cair.height;
            }
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = (FileStream)saveFileDialog1.OpenFile();
                toSave.Save(fs, ImageFormat.Png);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
