namespace DemoWinForms
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.labTitle = new System.Windows.Forms.Label();
            this.labClose = new System.Windows.Forms.Label();
            this.labSmall = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(1);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.labSmall);
            this.splitContainer1.Panel1.Controls.Add(this.labClose);
            this.splitContainer1.Panel1.Controls.Add(this.labTitle);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 0;
            // 
            // labTitle
            // 
            this.labTitle.AutoSize = true;
            this.labTitle.Location = new System.Drawing.Point(13, 13);
            this.labTitle.Name = "labTitle";
            this.labTitle.Size = new System.Drawing.Size(55, 15);
            this.labTitle.TabIndex = 0;
            this.labTitle.Text = "label1";
            // 
            // labClose
            // 
            this.labClose.AutoSize = true;
            this.labClose.Location = new System.Drawing.Point(773, 13);
            this.labClose.Name = "labClose";
            this.labClose.Size = new System.Drawing.Size(15, 15);
            this.labClose.TabIndex = 1;
            this.labClose.Text = "X";
            this.labClose.Click += new System.EventHandler(this.labClose_Click);
            // 
            // labSmall
            // 
            this.labSmall.AutoSize = true;
            this.labSmall.Location = new System.Drawing.Point(744, 13);
            this.labSmall.Name = "labSmall";
            this.labSmall.Size = new System.Drawing.Size(23, 15);
            this.labSmall.TabIndex = 2;
            this.labSmall.Text = "__";
            this.labSmall.Click += new System.EventHandler(this.labSmall_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label labTitle;
        private System.Windows.Forms.Label labSmall;
        private System.Windows.Forms.Label labClose;
    }
}

