namespace wsIntegracionGPS_BioSalc
{
    partial class GPS_Biosalc
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.niGPS = new System.Windows.Forms.NotifyIcon(this.components);
            this.eventLog1 = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).BeginInit();
            // 
            // niGPS
            // 
            this.niGPS.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.niGPS.Text = "Integración GPS-BioSalc";
            this.niGPS.Visible = true;
            // 
            // Service1
            // 
            this.ServiceName = "Service1";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).EndInit();

        }

        #endregion
        private System.Windows.Forms.NotifyIcon niGPS;
        private System.Diagnostics.EventLog eventLog1;
    }
}
