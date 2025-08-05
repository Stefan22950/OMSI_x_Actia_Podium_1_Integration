namespace OmsiVisualInterfaceNet
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            StopScreen = new Panel();
            Warning = new Label();
            pictureBox1 = new PictureBox();
            MainScreen = new Panel();
            ms_asr = new PictureBox();
            ms_secu = new PictureBox();
            ms_retarderOff = new PictureBox();
            ms_adBlue = new PictureBox();
            ms_fuel = new PictureBox();
            ms_coolant = new PictureBox();
            ms_retarder = new PictureBox();
            ms_ramp = new PictureBox();
            ms_dev = new PictureBox();
            ms_busStop = new PictureBox();
            ms_brake = new PictureBox();
            pictureBox2 = new PictureBox();
            LogoScreen = new Panel();
            pictureBox3 = new PictureBox();
            CoolantTemperatureScreen = new Panel();
            coolant_temp = new Panel();
            pictureBox4 = new PictureBox();
            FuelScreen = new Panel();
            adblue = new Panel();
            fuel = new Panel();
            pictureBox5 = new PictureBox();
            PressureScreen = new Panel();
            pressure_2 = new Panel();
            pressure_1 = new Panel();
            pictureBox6 = new PictureBox();
            StopScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            MainScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ms_asr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_secu).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_retarderOff).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_adBlue).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_fuel).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_coolant).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_retarder).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_ramp).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_dev).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_busStop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ms_brake).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            LogoScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            CoolantTemperatureScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            FuelScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).BeginInit();
            PressureScreen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox6).BeginInit();
            SuspendLayout();
            // 
            // StopScreen
            // 
            StopScreen.Controls.Add(Warning);
            StopScreen.Controls.Add(pictureBox1);
            StopScreen.Location = new Point(1220, 12);
            StopScreen.Name = "StopScreen";
            StopScreen.Size = new Size(803, 450);
            StopScreen.TabIndex = 0;
            // 
            // Warning
            // 
            Warning.AutoSize = true;
            Warning.BackColor = Color.FromArgb(216, 140, 43);
            Warning.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point);
            Warning.Location = new Point(18, 197);
            Warning.Name = "Warning";
            Warning.Size = new Size(59, 23);
            Warning.TabIndex = 1;
            Warning.Text = "label2";
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Image = Properties.Resources.actia_display_main_stop3;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(803, 450);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // MainScreen
            // 
            MainScreen.Controls.Add(ms_asr);
            MainScreen.Controls.Add(ms_secu);
            MainScreen.Controls.Add(ms_retarderOff);
            MainScreen.Controls.Add(ms_adBlue);
            MainScreen.Controls.Add(ms_fuel);
            MainScreen.Controls.Add(ms_coolant);
            MainScreen.Controls.Add(ms_retarder);
            MainScreen.Controls.Add(ms_ramp);
            MainScreen.Controls.Add(ms_dev);
            MainScreen.Controls.Add(ms_busStop);
            MainScreen.Controls.Add(ms_brake);
            MainScreen.Controls.Add(pictureBox2);
            MainScreen.Location = new Point(962, 468);
            MainScreen.Name = "MainScreen";
            MainScreen.Size = new Size(803, 450);
            MainScreen.TabIndex = 1;
            // 
            // ms_asr
            // 
            ms_asr.BackColor = Color.FromArgb(216, 140, 43);
            ms_asr.Enabled = false;
            ms_asr.Image = Properties.Resources.ms_icon11;
            ms_asr.Location = new Point(17, 228);
            ms_asr.Name = "ms_asr";
            ms_asr.Size = new Size(149, 104);
            ms_asr.SizeMode = PictureBoxSizeMode.Zoom;
            ms_asr.TabIndex = 11;
            ms_asr.TabStop = false;
            // 
            // ms_secu
            // 
            ms_secu.BackColor = Color.FromArgb(216, 140, 43);
            ms_secu.Enabled = false;
            ms_secu.Image = Properties.Resources.ms_icon6;
            ms_secu.Location = new Point(17, 118);
            ms_secu.Name = "ms_secu";
            ms_secu.Size = new Size(149, 104);
            ms_secu.SizeMode = PictureBoxSizeMode.Zoom;
            ms_secu.TabIndex = 10;
            ms_secu.TabStop = false;
            // 
            // ms_retarderOff
            // 
            ms_retarderOff.BackColor = Color.FromArgb(216, 140, 43);
            ms_retarderOff.Enabled = false;
            ms_retarderOff.Image = Properties.Resources.ms_icon10;
            ms_retarderOff.Location = new Point(638, 118);
            ms_retarderOff.Name = "ms_retarderOff";
            ms_retarderOff.Size = new Size(149, 104);
            ms_retarderOff.SizeMode = PictureBoxSizeMode.Zoom;
            ms_retarderOff.TabIndex = 9;
            ms_retarderOff.TabStop = false;
            // 
            // ms_adBlue
            // 
            ms_adBlue.BackColor = Color.FromArgb(216, 140, 43);
            ms_adBlue.Enabled = false;
            ms_adBlue.Image = Properties.Resources.ms_icon9;
            ms_adBlue.Location = new Point(482, 118);
            ms_adBlue.Name = "ms_adBlue";
            ms_adBlue.Size = new Size(149, 104);
            ms_adBlue.SizeMode = PictureBoxSizeMode.Zoom;
            ms_adBlue.TabIndex = 8;
            ms_adBlue.TabStop = false;
            // 
            // ms_fuel
            // 
            ms_fuel.BackColor = Color.FromArgb(216, 140, 43);
            ms_fuel.Enabled = false;
            ms_fuel.Image = Properties.Resources.ms_icon8;
            ms_fuel.Location = new Point(327, 118);
            ms_fuel.Name = "ms_fuel";
            ms_fuel.Size = new Size(149, 104);
            ms_fuel.SizeMode = PictureBoxSizeMode.Zoom;
            ms_fuel.TabIndex = 7;
            ms_fuel.TabStop = false;
            // 
            // ms_coolant
            // 
            ms_coolant.BackColor = Color.FromArgb(216, 140, 43);
            ms_coolant.Enabled = false;
            ms_coolant.Image = Properties.Resources.ms_icon7;
            ms_coolant.Location = new Point(172, 118);
            ms_coolant.Name = "ms_coolant";
            ms_coolant.Size = new Size(149, 104);
            ms_coolant.SizeMode = PictureBoxSizeMode.Zoom;
            ms_coolant.TabIndex = 6;
            ms_coolant.TabStop = false;
            // 
            // ms_retarder
            // 
            ms_retarder.BackColor = Color.FromArgb(216, 140, 43);
            ms_retarder.Enabled = false;
            ms_retarder.Image = Properties.Resources.ms_icon5;
            ms_retarder.Location = new Point(638, 8);
            ms_retarder.Name = "ms_retarder";
            ms_retarder.Size = new Size(149, 104);
            ms_retarder.SizeMode = PictureBoxSizeMode.Zoom;
            ms_retarder.TabIndex = 5;
            ms_retarder.TabStop = false;
            // 
            // ms_ramp
            // 
            ms_ramp.BackColor = Color.FromArgb(216, 140, 43);
            ms_ramp.Enabled = false;
            ms_ramp.Image = Properties.Resources.ms_icon4;
            ms_ramp.Location = new Point(482, 8);
            ms_ramp.Name = "ms_ramp";
            ms_ramp.Size = new Size(149, 104);
            ms_ramp.SizeMode = PictureBoxSizeMode.Zoom;
            ms_ramp.TabIndex = 4;
            ms_ramp.TabStop = false;
            // 
            // ms_dev
            // 
            ms_dev.BackColor = Color.FromArgb(216, 140, 43);
            ms_dev.Enabled = false;
            ms_dev.Image = Properties.Resources.ms_icon3;
            ms_dev.Location = new Point(327, 8);
            ms_dev.Name = "ms_dev";
            ms_dev.Size = new Size(149, 104);
            ms_dev.SizeMode = PictureBoxSizeMode.Zoom;
            ms_dev.TabIndex = 3;
            ms_dev.TabStop = false;
            // 
            // ms_busStop
            // 
            ms_busStop.BackColor = Color.FromArgb(216, 140, 43);
            ms_busStop.Enabled = false;
            ms_busStop.Image = Properties.Resources.ms_icon2;
            ms_busStop.Location = new Point(172, 8);
            ms_busStop.Name = "ms_busStop";
            ms_busStop.Size = new Size(149, 104);
            ms_busStop.SizeMode = PictureBoxSizeMode.Zoom;
            ms_busStop.TabIndex = 2;
            ms_busStop.TabStop = false;
            // 
            // ms_brake
            // 
            ms_brake.BackColor = Color.FromArgb(216, 140, 43);
            ms_brake.Enabled = false;
            ms_brake.Image = Properties.Resources.ms_icon1;
            ms_brake.Location = new Point(17, 8);
            ms_brake.Name = "ms_brake";
            ms_brake.Size = new Size(149, 104);
            ms_brake.SizeMode = PictureBoxSizeMode.Zoom;
            ms_brake.TabIndex = 1;
            ms_brake.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.Dock = DockStyle.Fill;
            pictureBox2.Image = Properties.Resources.actia_display_main;
            pictureBox2.Location = new Point(0, 0);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(803, 450);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 0;
            pictureBox2.TabStop = false;
            // 
            // LogoScreen
            // 
            LogoScreen.Controls.Add(pictureBox3);
            LogoScreen.Location = new Point(1168, 102);
            LogoScreen.Name = "LogoScreen";
            LogoScreen.Size = new Size(803, 450);
            LogoScreen.TabIndex = 2;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = Properties.Resources.actia_display_startuplogo;
            pictureBox3.Location = new Point(0, 0);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(803, 450);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.TabIndex = 0;
            pictureBox3.TabStop = false;
            // 
            // CoolantTemperatureScreen
            // 
            CoolantTemperatureScreen.Controls.Add(coolant_temp);
            CoolantTemperatureScreen.Controls.Add(pictureBox4);
            CoolantTemperatureScreen.Location = new Point(1050, 235);
            CoolantTemperatureScreen.Name = "CoolantTemperatureScreen";
            CoolantTemperatureScreen.Size = new Size(803, 450);
            CoolantTemperatureScreen.TabIndex = 3;
            // 
            // coolant_temp
            // 
            coolant_temp.Location = new Point(59, 273);
            coolant_temp.Name = "coolant_temp";
            coolant_temp.Size = new Size(8, 45);
            coolant_temp.TabIndex = 1;
            // 
            // pictureBox4
            // 
            pictureBox4.Image = Properties.Resources.actia_display_Info_watertemp;
            pictureBox4.Location = new Point(0, 0);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(803, 450);
            pictureBox4.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox4.TabIndex = 0;
            pictureBox4.TabStop = false;
            // 
            // FuelScreen
            // 
            FuelScreen.Controls.Add(adblue);
            FuelScreen.Controls.Add(fuel);
            FuelScreen.Controls.Add(pictureBox5);
            FuelScreen.Location = new Point(928, 20);
            FuelScreen.Name = "FuelScreen";
            FuelScreen.Size = new Size(803, 450);
            FuelScreen.TabIndex = 4;
            // 
            // adblue
            // 
            adblue.Location = new Point(94, 383);
            adblue.Name = "adblue";
            adblue.Size = new Size(8, 45);
            adblue.TabIndex = 2;
            // 
            // fuel
            // 
            fuel.Location = new Point(94, 160);
            fuel.Name = "fuel";
            fuel.Size = new Size(8, 45);
            fuel.TabIndex = 1;
            // 
            // pictureBox5
            // 
            pictureBox5.Image = Properties.Resources.actia_display_Info_fuel;
            pictureBox5.Location = new Point(0, 0);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(800, 450);
            pictureBox5.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox5.TabIndex = 0;
            pictureBox5.TabStop = false;
            // 
            // PressureScreen
            // 
            PressureScreen.Controls.Add(pressure_2);
            PressureScreen.Controls.Add(pressure_1);
            PressureScreen.Controls.Add(pictureBox6);
            PressureScreen.Location = new Point(49, 89);
            PressureScreen.Name = "PressureScreen";
            PressureScreen.Size = new Size(803, 450);
            PressureScreen.TabIndex = 5;
            // 
            // pressure_2
            // 
            pressure_2.Location = new Point(54, 381);
            pressure_2.Name = "pressure_2";
            pressure_2.Size = new Size(8, 45);
            pressure_2.TabIndex = 2;
            // 
            // pressure_1
            // 
            pressure_1.Location = new Point(54, 271);
            pressure_1.Name = "pressure_1";
            pressure_1.Size = new Size(8, 45);
            pressure_1.TabIndex = 1;
            // 
            // pictureBox6
            // 
            pictureBox6.Image = Properties.Resources.actia_display_Info_airpressure;
            pictureBox6.Location = new Point(0, 0);
            pictureBox6.Name = "pictureBox6";
            pictureBox6.Size = new Size(800, 450);
            pictureBox6.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox6.TabIndex = 0;
            pictureBox6.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1347, 678);
            Controls.Add(FuelScreen);
            Controls.Add(PressureScreen);
            Controls.Add(CoolantTemperatureScreen);
            Controls.Add(LogoScreen);
            Controls.Add(StopScreen);
            Controls.Add(MainScreen);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            StopScreen.ResumeLayout(false);
            StopScreen.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            MainScreen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ms_asr).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_secu).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_retarderOff).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_adBlue).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_fuel).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_coolant).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_retarder).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_ramp).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_dev).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_busStop).EndInit();
            ((System.ComponentModel.ISupportInitialize)ms_brake).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            LogoScreen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            CoolantTemperatureScreen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            FuelScreen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox5).EndInit();
            PressureScreen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox6).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel StopScreen;
        private PictureBox pictureBox1;
        private Panel MainScreen;
        private PictureBox pictureBox2;
        private Panel LogoScreen;
        private PictureBox pictureBox3;
        private Label Warning;
        private PictureBox ms_brake;
        private PictureBox ms_retarder;
        private PictureBox ms_ramp;
        private PictureBox ms_dev;
        private PictureBox ms_busStop;
        private PictureBox ms_asr;
        private PictureBox ms_secu;
        private PictureBox ms_retarderOff;
        private PictureBox ms_adBlue;
        private PictureBox ms_fuel;
        private PictureBox ms_coolant;
        private Panel CoolantTemperatureScreen;
        private PictureBox pictureBox4;
        private Panel coolant_temp;
        private Panel FuelScreen;
        private Panel fuel;
        private PictureBox pictureBox5;
        private Panel adblue;
        private Panel PressureScreen;
        private Panel pressure_2;
        private Panel pressure_1;
        private PictureBox pictureBox6;
    }
}
