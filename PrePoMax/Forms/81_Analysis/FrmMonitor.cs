using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeJob;

namespace PrePoMax.Forms
{
    public partial class FrmMonitor : UserControls.PrePoMaxChildForm
    {
        // Variables                                                                                                                
        private AnalysisJob _job;


        // Properties                                                                                                               


        // Events                                                                                                                   
        public event Action<string> KillJob;
        public event Action<string> Results;


        // Constructors                                                                                                             
        public FrmMonitor()
        {
            InitializeComponent();
        }


        // Event handlers                                                                                                           
        private void FrmMonitor_Shown(object sender, EventArgs e)
        {
            tbOutput.Select(tbOutput.TextLength, 0);
            tbOutput.ScrollToCaret();
        }
        private void FrmMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_job != null)
            {
                _job.DataOutput -= UpdateOutput;
                _job = null;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                Hide();
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_job.JobStatus == JobStatus.Running)
            {
                if (tabControl1.SelectedTab == tpOutput)
                {
                    tbOutput.Select(tbOutput.TextLength, 0);
                    tbOutput.ScrollToCaret();
                }
                else if (tabControl1.SelectedTab == tpStatus)
                {
                    tbStatus.Select(tbOutput.TextLength, 0);
                    tbStatus.ScrollToCaret();
                }
                else if (tabControl1.SelectedTab == tpCovergence)
                {
                    tbConvergence.Select(tbOutput.TextLength, 0);
                    tbConvergence.ScrollToCaret();
                }
            }
        }

        private void btnKill_Click(object sender, EventArgs e)
        {
            try
            {
                if (KillJob != null) KillJob(_job.Name);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }

        private void btnResults_Click(object sender, EventArgs e)
        {
            try
            {
                // function results hides this form if everything ok
                if (Results != null) Results(_job.Name);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (_job != null)
            {
                _job.DataOutput -= UpdateOutput;
                _job = null;
            }
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Hide();
        }

        // Methods                                                                                                                  
        public void PrepareForm(AnalysisJob job)
        {
            this.DialogResult = DialogResult.None;      // to prevent the call to frmMain.itemForm_VisibleChanged when minimized
            //
            _job = job;
            _job.DataOutput += UpdateOutput;
            //
            UpdateProgress();
            //
            tbOutput.Text = _job.AllOutputData;
            tbOutput.Select(tbOutput.TextLength, 0);
            tbOutput.ScrollToCaret();
            //
            tbStatus.Text = _job.StatusFileData;
            tbStatus.Select(tbOutput.TextLength, 0);
            tbStatus.ScrollToCaret();
        }

        private void UpdateOutput()
        {
            try
            {
                if (this.tbOutput.InvokeRequired)
                {
                    // This is a worker thread so delegate the task.
                    this.BeginInvoke(new MethodInvoker(() => UpdateOutput()));
                }
                else
                {
                    // It's on the same thread, no need for Invoke
                    tbOutput.AutoScrollAppendText(_job.OutputData);
                    //
                    tbStatus.AutoScrollSetText(_job.StatusFileData);
                    //
                    tbConvergence.AutoScrollSetText(_job.ConvergenceFileData);
                }
            }
            catch
            { }
        }
        public void UpdateProgress()
        {
            if (_job != null)
            {
                if (_job.JobStatus == JobStatus.Running)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Marquee;
                    labAnalysisStatus.Text = "      " + _job.JobStatus.ToString();
                    labAnalysisStatus.Image = global::PrePoMax.Properties.Resources.Running;
                }
                else if (_job.JobStatus == JobStatus.OK)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + "Finished";
                    labAnalysisStatus.Image = global::PrePoMax.Properties.Resources.OK;
                }
                else if (_job.JobStatus == JobStatus.FailedWithResults)
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + "Failed with results";
                    labAnalysisStatus.Image = global::PrePoMax.Properties.Resources.Warning;
                }
                else
                {
                    pbAnalysisStatus.Style = ProgressBarStyle.Blocks;
                    labAnalysisStatus.Text = "      " + _job.JobStatus.ToString();
                    labAnalysisStatus.Image = global::PrePoMax.Properties.Resources.NoResult;
                }
            }
        }
    }
}
