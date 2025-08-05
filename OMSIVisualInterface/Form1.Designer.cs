namespace OMSIVisualInterface
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelRpm = new System.Windows.Forms.Label();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelRpm
            // 
            this.labelRpm.AutoSize = true;
            this.labelRpm.Location = new System.Drawing.Point(39, 33);
            this.labelRpm.Name = "labelRpm";
            this.labelRpm.Size = new System.Drawing.Size(44, 16);
            this.labelRpm.TabIndex = 0;
            this.labelRpm.Text = "label1";
            // 
            // labelSpeed
            // 
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.Location = new System.Drawing.Point(39, 80);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(44, 16);
            this.labelSpeed.TabIndex = 1;
            this.labelSpeed.Text = "label2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(140)))), ((int)(((byte)(43)))));
            this.ClientSize = new System.Drawing.Size(302, 193);
            this.Controls.Add(this.labelSpeed);
            this.Controls.Add(this.labelRpm);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelRpm;
        private System.Windows.Forms.Label labelSpeed;
    }
}

