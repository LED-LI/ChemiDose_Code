using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Runtime.InteropServices;
//using BCL.easyPDF.Printer;
using Microsoft.Win32;

namespace SpaceUSB
{
    public partial class CMForm : Form
    {
        //  all the following directories will be under the base directory:

        string pcCode = "2022-06-13";

        string cmPath = "C:\\cmRUN\\";    // base directory. can be changed by the user
        string logPath = "logfiles\\";
        string setupPath = "setupfiles\\";
        string cmRUNpath = "cmFiles\\";
        string logFileName = "cmLogs.txt";
        string lastSetupName = "lastSetup.txt";
        string SetupName = "Setup.txt";
        string backupPath = "TrinamicBackupFiles\\";       // backupFiles\\";
                                                           //        string backupFileName = "Backup";                  // 2021-08-25 09-52 Backup sn 1030002 sw 1030002.txt
        string cmFileNameEnd = " cmRUN.txt";

        string paramsPath = "paramsFiles\\";
        string paramsFileName = "cmParams.txt";

        string pwPath = "pwFiles\\";
        string pwFileName = "cmPW.txt";

        //string pdfPath = "pdfFiles\\";       // file names acording to the .txt files

        string username;
        string userPassWord;
        string pwMaster;
        //string runDay;
        //string cmFile;
        string curentPrintFile;
        double v;
        public bool isAdministrator = false;      // start program with non-administrator
                                                  //        bool calibrateInProcess;
        bool anyErrorGotTrue = false;
        public bool readyForNewCommand;
        public bool calibrating = false;
        public bool RunInProcess = false;
        public bool cmdInProcess;
        public bool motorIsMoving;
        public bool homingDone;
        public bool anyError;
        public bool stopOnError = false;
        public bool nextTipInProcess;
        public bool movingToBottle;
        public bool aborted = false;
        public int microLdispensedSoFar;

        // *** E R R O R S ***
        public int errorsSyringeBag;
        public int errorsM_Vertical;
        public int errorsM_Linear;
        public int errorsM_Arm;
        public int errorsM_Piston;
        public int errorsM_HeadRotate;
        public int errorsM_Disposee;
        public int errorsM_CapHolder;

        public int errors_Vial_1;
        public int errors_Vial_2;
        public int errors_Vial_3;
        public int errors_Vial_4;
        public int errors_Vial_5;
        public int errors_Vial_6;

        public int errors_findHome;
        public int errors_wrong_PC_command;
        public int special_Error;
        public int NeedleGauge;
        public int NeedleLength;
        public int errorsWrongPCcmd;
        public int TrinamicCode;
        public int TrinamicSerialNum;
        public int CyclesTotal;
        public int activeBottle;
        public int leftTries;
        public int getGBresult;
        public int unitsToMove;
        public int LoadingHight;
        public int linearHomePos;
        public int ArmHomePosition;
        public int PistonHomePos;
        public int HeadRotateHomePos;
        public int DisposeHomePos;
        public int CapHolderHomePos;
        public int microLitterInBag;
        public int Vial1Volume;
        public int Vial2Volume;
        public int Vial3Volume;
        public int Vial4Volume;
        public int Vial5Volume;
        public int Vial6Volume;
        public int vibrating4Done;
        public int vibrating56Done;
        public int vibrationTime4;
        public int vibrationTime56;
        public int vibrationHz;
        public int vibrationStrength;
        public int vialsExist;
        public bool okPolling = true;
        // *** vials exist bits ***
        public int Bit_vial1 = 0b00000001; // bit   1
        public int Bit_vial2 = 0b00000010; // bit   2
        public int Bit_vial3 = 0b00000100; // bit   4
        public int Bit_vial4 = 0b00001000; // bit   8
        public int Bit_vial5 = 0b00010000; // bit  16
        public int Bit_vial6 = 0b00100000; // bit  32
        public int Bit_bag = 0b01000000; // bit  64

        int maxPWtrials = 4;
        int maxPWmonths = 5;
        int percAddP100 = 25;
        int maxGracePWdays = 16;
        int currentTAB = 0;
        Regex rgNumber = new Regex("^[0-9]+$");      // regular expression: from the start of string, any number, multiple, end of string
        Regex rgMinus = new Regex("^-?[0-9]+$");     // regular expression: from the start of string, posible minus, any number, multiple, end of string
        Regex rgEmpty = new Regex("^[-, ]?[0-9]+$"); // regular expression: from the start of string, posible minus, any number, multiple, end of string

        private TMCConn rTMCConn;
        private Thread updateThread;
        Response tResponse = new Response();
        string tString = "";
        public CMForm()
        {
            InitializeComponent();
            visibleFalse();
            //ThreadsClass tc = new ThreadsClass(this);   // for pointer of Form1
            checkDirectories();
            readParamsFile();
            setRegeditNotepadTextsize80();
            // following lines for testing, without pw
            isAdministrator = true;
            visibleMaster();

            if (!File.Exists(cmPath + pwPath + pwFileName))     // password file exists?
            {
                logAndShow("PW file does not exist. Try master PW");
                PWfileEmptyPnl.Visible = true;          // enable master PW entrance, PW = DATE
                userPWtlp.Visible = false;
                return;
            }
            rTMCConn = new TMCConn();
            updateThread = new Thread(ThreadPollTrinamic);
            updateThread.Start();

        }

        public void ThreadPollTrinamic()   // this is the thread that polls the motors positions 
                                           // and end Trinamic commands
        {
            // https://stackoverflow.com/questions/661561/how-do-i-update-the-gui-from-another-thread

            while (true)
            {
                if (!rTMCConn.TrinamicOK)
                {
                    return;
                }
                tResponse = rTMCConn.GetGAP(MotorsNum.M_Vertical, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_VerticalStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_VerticalLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGAP(MotorsNum.M_Linear, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_LinearStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_LinearLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGAP(MotorsNum.M_Arm, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_ArmStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_ArmLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGAP(MotorsNum.M_Piston, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_PistonStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_PistonLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGAP(MotorsNum.M_HeadRotate, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_RotateStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_headRotateLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGAP(MotorsNum.M_Dispose, AddressBank.actualPosition);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_disposeMicroStepsPerMM;
                this.Invoke((MethodInvoker)delegate { M_DisposeLocationTb.Text = $"{v,10:0.00}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHolds);
                v = Convert.ToDouble(tResponse.tmcReply.value) / StepsPerMM.M_capHolderMicroStepsPerMM;
                if (v == 0)
                {
                    this.Invoke((MethodInvoker)delegate { M_CapHolderLocationTB.Text = $"O P E N"; });
                }
                else // close
                {
                    this.Invoke((MethodInvoker)delegate { M_CapHolderLocationTB.Text = $"C L O S E"; });
                }

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_CmdInProcess);
                cmdInProcess = Convert.ToBoolean(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { CmdInProcTB.Text = $"{cmdInProcess}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_motorIsMoving);
                motorIsMoving = Convert.ToBoolean(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { MotorIsMovingTB.Text = $"{motorIsMoving}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_HomingDone);
                homingDone = Convert.ToBoolean(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { HomingDoneTB.Text = $"{homingDone}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_any_Error);
                anyError = Convert.ToBoolean(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { AnyErrorTB.Text = $"{anyError}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_syringe_bag);
                errorsSyringeBag = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { SyringeBagErrorsTB.Text = Convert.ToString(errorsSyringeBag, 2); });
                this.Invoke((MethodInvoker)delegate { BagErrorTB.Text = Convert.ToString(errorsSyringeBag, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_verticalMotor);
                errorsM_Vertical = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_VerticalErrorsTB.Text = Convert.ToString(errorsM_Vertical, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_linearMotor);
                errorsM_Linear = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_LinearErrorsTB.Text = Convert.ToString(errorsM_Linear, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_armMotor);
                errorsM_Arm = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_ArmErrorsTB.Text = Convert.ToString(errorsM_Arm, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_pistonMotor);
                errorsM_Piston = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_PistonErrorsTB.Text = Convert.ToString(errorsM_Piston, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_headRotateMotor);
                errorsM_HeadRotate = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_HeadRotateErrorsTB.Text = Convert.ToString(errorsM_HeadRotate, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_disposeMotor);
                errorsM_Disposee = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_DisposeErrorsTB.Text = Convert.ToString(errorsM_Disposee, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_M_capHolderMotor);
                errorsM_CapHolder = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { M_CapHolderErrorsTB.Text = Convert.ToString(errorsM_CapHolder, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_wrong_PC_command);
                errorsWrongPCcmd = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { WrongPcErrorsTB.Text = Convert.ToString(errorsWrongPCcmd, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_findHome);
                errors_findHome = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { FindHomeErrorsTB.Text = Convert.ToString(errors_findHome, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_special_Error);
                special_Error = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { SpecialErrorsTB.Text = Convert.ToString(special_Error, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_1);
                errors_Vial_1 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial1ErrTB.Text = Convert.ToString(errors_Vial_1, 2); });
                this.Invoke((MethodInvoker)delegate { Vial1ErrorTB.Text = Convert.ToString(errors_Vial_1, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_2);
                errors_Vial_2 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial2ErrTB.Text = Convert.ToString(errors_Vial_2, 2); });
                this.Invoke((MethodInvoker)delegate { Vial2ErrorTB.Text = Convert.ToString(errors_Vial_2, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_3);
                errors_Vial_3 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial3ErrTB.Text = Convert.ToString(errors_Vial_3, 2); });
                this.Invoke((MethodInvoker)delegate { Vial3ErrorTB.Text = Convert.ToString(errors_Vial_3, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_4);
                errors_Vial_4 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial4ErrTB.Text = Convert.ToString(errors_Vial_4, 2); });
                this.Invoke((MethodInvoker)delegate { Vial4ErrorTB.Text = Convert.ToString(errors_Vial_4, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_5);
                errors_Vial_5 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial5ErrTB.Text = Convert.ToString(errors_Vial_5, 2); });
                this.Invoke((MethodInvoker)delegate { Vial5ErrorTB.Text = Convert.ToString(errors_Vial_5, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_errors_Vial_6);
                errors_Vial_6 = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { Vial6ErrTB.Text = Convert.ToString(errors_Vial_6, 2); });
                this.Invoke((MethodInvoker)delegate { Vial6ErrorTB.Text = Convert.ToString(errors_Vial_6, 2); });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_currentTrinamicSWversion);
                TrinamicCode = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { TrinamicCodeTB.Text = $"{TrinamicCode}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_HardwareSerialNumber); // get serial #
                TrinamicSerialNum = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { robotSerialTB.Text = $"{TrinamicSerialNum}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_cyclesTotal);
                CyclesTotal = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { CyclesTotalTB.Text = $"{CyclesTotal}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, getGBnumberTB.Text);
                getGBresult = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { getGBresultTB.Text = $"{getGBresult}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLitterInBAG);  //GB_99
                microLitterInBag = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinBagTB.Text = $"{microLitterInBag}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial1);  //GB_197
                Vial1Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinVial1TB.Text = $"{Vial1Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial2);  //GB_198
                Vial2Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinVial2TB.Text = $"{Vial2Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial3);  //GB_199
                Vial3Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinVial3TB.Text = $"{Vial3Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial4);  //GB_200
                Vial4Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinVial4TB.Text = $"{Vial4Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial5);  //GB_201
                Vial5Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { microLinVial5TB.Text = $"{Vial5Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_microLinVial6);  //GB_202
                Vial6Volume = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { isVibrating4TB.Text = $"{Vial6Volume}"; });

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrator4done);  //GB_121
                vibrating4Done = Convert.ToInt32(tResponse.tmcReply.value);
                if (vibrating4Done == 0)
                {
                    this.Invoke((MethodInvoker)delegate { isVibrating4TB.Text = $"VIBRATING"; });
                }
                else // ==1
                {
                    this.Invoke((MethodInvoker)delegate { isVibrating4TB.Text = $""; });
                }

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrator56done);  //GB_122
                vibrating56Done = Convert.ToInt32(tResponse.tmcReply.value);
                if (vibrating56Done == 0)
                {
                    this.Invoke((MethodInvoker)delegate { isVibrating56TB.Text = $"VIBRATING"; });
                }
                else // ==1
                {
                    this.Invoke((MethodInvoker)delegate { isVibrating56TB.Text = $""; });
                }

                this.Invoke((MethodInvoker)delegate { runInProcessTB.Text = $"{RunInProcess}"; });
                this.Invoke((MethodInvoker)delegate { DateTimeNowTxt.Text = DateTime.Now.ToString("  yyyy-MM-dd   HH:mm:ss"); });
                this.Invoke((MethodInvoker)delegate { PcCodeTB.Text = pcCode; });

                readyForNewCommand = !cmdInProcess && !motorIsMoving && !anyError; ; // && !movingToBottle;

                if (!anyError)
                {
                    anyErrorGotTrue = false;   // reset the next message
                }
                ErrorsLog();
                anyErrorGotTrue = anyError;

                // get vials sensors
                tResponse = rTMCConn.RunCommand(GeneralFunctions.screenAllVials);
                tstringToRUNtest();    // display on "for RUN cmd"

                Thread.Sleep(100);   // wait 100 ms for the program to finish to switch and thread sleep

                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vialsExist);  //GB_98
                vialsExist = Convert.ToInt32(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { vialsExistTB.Text = Convert.ToString(vialsExist, 2).PadLeft(7, '0'); });

                if ((vialsExist & Bit_bag) == Bit_bag)
                {
                    this.Invoke((MethodInvoker)delegate { BagInPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { BagInPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial1) == Bit_vial1)
                {
                    this.Invoke((MethodInvoker)delegate { Vial1InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial1InPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial2) == Bit_vial2)
                {
                    this.Invoke((MethodInvoker)delegate { Vial2InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial2InPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial3) == Bit_vial3)
                {
                    this.Invoke((MethodInvoker)delegate { Vial3InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial3InPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial4) == Bit_vial4)
                {
                    this.Invoke((MethodInvoker)delegate { Vial4InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial4InPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial5) == Bit_vial5)
                {
                    this.Invoke((MethodInvoker)delegate { Vial5InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial5InPlaceTB.Text = ""; });
                }
                if ((vialsExist & Bit_vial6) == Bit_vial6)
                {
                    this.Invoke((MethodInvoker)delegate { Vial6InPlaceTB.Text = "In Place"; });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { Vial6InPlaceTB.Text = ""; });
                }
            }
        }
        private void tstringToSGPtest()    // SGP commands
        {
            try
            {
                //if (tResponse.tmcReply == null)
                //{
                //    return;
                //}
                tString = Convert.ToString(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { SGPtestTB.Text = tString; });
            }
            catch (Exception e)
            {
                //logAndShow($"Error writing to 'for SGP cmd'");
                logAndShow(e.Message);
                return;
            }
        }
        private void tstringToRUNtest()    // RUN commands
        {
            if (aborted)
            {
                return;
            }
            try
            {
                tString = Convert.ToString(tResponse.tmcReply.value);
                this.Invoke((MethodInvoker)delegate { RUNtestTB.Text = tString; });
                tString = Convert.ToString(tResponse.tmcReply.status);
                this.Invoke((MethodInvoker)delegate { replyStatusTB.Text = tString; });
            }
            catch (Exception e)
            {
                logAndShow(e.Message);
                return;
            }
        }
        private void tstringDiv1000()   // move command
        {
            if (aborted)
            {
                //                return;
            }
            tString = Convert.ToString(Convert.ToDouble(tResponse.tmcReply.value) / 1000);
            this.Invoke((MethodInvoker)delegate { movingTB.Text = tString; });
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        //=============
        // E R R O R S
        //=============
        private void ErrorsLog()
        {
            if (!anyErrorGotTrue && anyError)    // will happen for one cycle after anyError was set
            {
                stopOnError = true;  // ok to run cycle *** stop running;

                if (errorsSyringeBag > 0)
                {
                    if (errorsSyringeBag == 32)
                    {
                        logAndShow
                        (
                          $"An Error occured in the robot:\r" +
                          $"==========================\r\r" +
                          $"\t machine was aborted \r\r" +
                          $"\t 1- run HOME  \r" +
                          $"\t 2- click RUN \r"
                        );
                    }
                    else
                    {
                        logAndShow
                        (
                          $"An Error occured in the robot:\r" +
                          $"====================\r\r" +
                          $"Syringe:  {errorsSyringeBag}\r" +
                          $"\tbagIsMissing =\t       1\r" +
                          $"\tsyringePoppedOut =\t       2\r" +
                          $"\tmachineAborted =\t      32\r" +
                          $"_______________________ \r"
                        );
                    }
                }
                // *** MOTORS Errors ***
                if (errorsM_Vertical != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Vertical motor:\t{errorsM_Vertical}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_Linear != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Linear motor:\t{errorsM_Linear}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_Arm != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Arm motor:\t{errorsM_Arm}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_Piston != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Piston motor:\t{errorsM_Piston}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_HeadRotate != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Head rotating motor:\t{errorsM_HeadRotate}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_Disposee != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Dispose motor:\t{errorsM_HeadRotate}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsM_CapHolder != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"Cap Holder motor:\t{errorsM_HeadRotate}   TimeOut\r\r" +
                        $"_______________________ \r"
                    );
                }
                // *** VIALs errors ***
                if (errors_Vial_1 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_1:\t{errors_Vial_1} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errors_Vial_2 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_2:\t{errors_Vial_2} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errors_Vial_3 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_3:\t{errors_Vial_3} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errors_Vial_4 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_4:\t{errors_Vial_4} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errors_Vial_5 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_5:\t{errors_Vial_5} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errors_Vial_6 != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"errors Vial_6:\t{errors_Vial_6} \r\r" +
                        $"\tVialMissing =\t 2\r" +
                        $"\tVialPoppedOut =\t 4\r" +
                        $"_______________________ \r"
                    );
                }

                // *** FIND HOME Errors ***
                if (errors_findHome != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"wrong PC cmd:\t{errors_findHome}\r\r" +
                        $"\tsyringeIsInwhileFindHome =\t 1\r" +
                        $"\texpecting_WAITING_DISPENSE =\t 4\r" +
                        $"_______________________ \r"
                    );
                }
                if (errorsWrongPCcmd != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"wrong PC cmd:\t{errorsWrongPCcmd}\r\r" +
                        $"\texpecting_GP58_10_OR_30 =\t 1\r" +
                        $"\texpecting_WAITING_DISPENSE =\t 2\r" +
                        $"\tvibrateParemeterError =\t 8\r" +
                        $"_______________________ \r"
                    );
                }
                if (special_Error != 0)
                {
                    logAndShow
                    (
                    $"An Error occured in the robot:\r" +
                    $"====================\r\r" +
                        $"wrong PC cmd:\t{special_Error}\r\r" +
                        $"\tSyringeIsIn =\t 1\r" +
                        $"\tSyringeMissing =\t 2\r" +
                        $"\tNo vials =\t 4\r" +
                        $"\tvolumeExceedsLimits =\t 8\r" +
                        $"_______________________ \r"
                    );
                }
            }
        }
        //======================
        // write R U N results
        //======================
        private void WriteCMrun()   // *****  create CM file for previous runs   ************
        {
            int i;
            string toDay;
            string strWithraw;
            string strSize;
            string fileName;
            Control dd = new Control();
            Control ff = new Control();

            // if (!readyForNewCommand)
            // {
            //    logAndShow("The robot is busy");
            //    return;
            //}
            //DialogResult dr1 = MessageBox.Show("The data will be copied to a file and erased from here. \n" +
            //                                   "Do you want to save and erase?",
            //                                   "erase display?", MessageBoxButtons.YesNo);
            //if (dr1 == DialogResult.No)
            //{
            //    return;  // exit
            //}
            // save file
            toDay = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
            fileName = cmPath + cmRUNpath + toDay + cmFileNameEnd;

            if (File.Exists(fileName))
            {
                DialogResult dr2 = MessageBox.Show($"The file: \'{fileName}\'exists. \n" +
                                                   "Do you want to over Write it?",
                                                   "overwrite existing file?", MessageBoxButtons.YesNo);
                if (dr2 == DialogResult.No)
                {
                    return;  // exit
                }
            }
            string toWrite = " CM bag RUN report\n"
                           + "\n Date: " + toDay
                           + "\n user: " + username + "\n\n"
                           + "==============================\n"
                           + "  Bag Size    Bag Volume [uL] \n"
                           + $"   {BagSizeUlTB.Text:10} {microLinBagTB.Text,24} \n\n"
                           + "  Vial#   Vial size  volume [uL] \n"
                           + "==============================\n\n";
            File.WriteAllText(fileName, toWrite);
            this.Invoke((MethodInvoker)delegate { microLinBagTB.Text = ""; });

            for (i = 1; i <= 6; i++)                                // go over 18 bottles
            {
                strSize = $"Vial{i:D1}SizeUlTB";                           // 1 2 3 volume column
                foreach (Control d in RunParametersTLP.Controls)
                {
                    if (d is TextBox && string.Equals(strSize, d.Name))
                    {
                        dd = d;
                    }
                }
                strWithraw = $"Vial{i:D1}WithdrawTB";                           // 1 2 3 volume column
                foreach (Control f in RunParametersTLP.Controls)
                {
                    if (f is TextBox && string.Equals(strWithraw, f.Name))
                    {
                        ff = f;
                    }
                }
                if ((dd.Text != "0" && dd.Text != "") || ff.Text != "")
                {
                    File.AppendAllText(fileName, $" {i,8} {dd.Text,14} {ff.Text,12} \n");
                }
            }
            // erase all columns
            foreach (Control k in RunParametersTLP.Controls)
            {
                if (k is TextBox)
                {
                    //this.Invoke((MethodInvoker)delegate { k.Text = ""; });
                    //k.Text = "";                                // erase all "volumes"
                }
            }

            // CreatePdfFile(cmRUNpath + runDay + cmFileNameEnd);
            // JustShow($"printed \"{runDay + cmFileNameEnd}\" file");
            // CreatePdfFile(cmRUNpath + toDay + " final" + cmFileNameEnd);
            //logAndShow($"printed \"{toDay + " final" + cmFileNameEnd}\" file");
            logAndShow($"printed \"{fileName}\" file");
            // viewCMfiles();
        }

        // =========================
        //      write Setup file    
        // =========================
        private void SaveBtn_Click(object sender, EventArgs e)
        {
            string strSize;
            string strWithraw;
            string fileNameLast = setupPath + lastSetupName;
            string fileNameDate = setupPath + DateTime.Now.ToString("yyyy-MM-dd HH-mm ") + SetupName;
            uint i;
            Control dd = new Control();
            Control ff = new Control();

            File.WriteAllText(cmPath + fileNameLast,
                            " Last Setup file,  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");

            File.WriteAllText(cmPath + fileNameDate,
                            " Setup file,  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");

            string toWrite = " User: " + username + "\n\n"
                            + "==============================\n"
                            + "  Bag Size[uL]     \n"
                            + $"{BagSizeUlTB.Text} \n\n"
                            + "  Vial#   Vial size  volume [uL] \n"
                            + "==============================\n";

            for (i = 1; i <= 6; i++)                                // go over 18 bottles
            {
                strSize = $"Vial{i:D1}SizeUlTB";                           // 1 2 3 volume column
                strWithraw = $"Vial{i:D1}WithdrawTB";                           // 1 2 3 volume column
                foreach (Control d in RunParametersTLP.Controls)
                {
                    if (d is TextBox && string.Equals(strSize, d.Name))
                    {
                        if (d.Text == "")
                        {
                            d.Text = "0";
                        }
                        dd = d;
                    }
                    if (d is TextBox && string.Equals(strWithraw, d.Name))
                    {
                        if (d.Text == "")
                        {
                            d.Text = "0";
                        }
                        ff = d;
                    }
                }
                if ((dd.Text != "0" && dd.Text != "") || ff.Text != "")
                {
                    toWrite += $"{i} {dd.Text} {ff.Text} \n";
                }
            }
            // write vibration data
            toWrite += $"\n Vibration parameters: time of vial 4, time of vials 56, HZ, Strength\n";
            toWrite += $"{vibrationTime4TB.Text} {vibrationTime56TB.Text} {vibrationHzTB.Text} {vibrationStrengthTB.Text}\n";

            File.AppendAllText(cmPath + fileNameLast, toWrite);
            File.AppendAllText(cmPath + fileNameDate, toWrite);

            writeLogFile(fileNameLast + "  written ");
            writeLogFile(fileNameDate + "  written \n");
        }

        // ======================
        //      last Setup button
        // ======================
        private void loadSetupsBtn_Click(object sender, EventArgs e)
        {
            int i;
            int j;
            string strSize;
            string strWithraw;
            string line;
            string vial;
            string sizeOfVial;
            string volume;
            string[] result; // = new string[16];
            //string str;
            //uint currentvial;

            openFileDialog1.InitialDirectory = cmPath + setupPath;
            openFileDialog1.Filter = "text files (*Setup.txt)|*Setup.txt";
            DialogResult dr = openFileDialog1.ShowDialog();  // choose the directory from file list 
            if (dr == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                for (i = 0; i < 5; i++)  // wait for first lines
                {
                    sr.ReadLine();
                }

                // read bag
                line = sr.ReadLine();
                result = line.Split(' ');          // BagSize = result[0], BagVolume = result[1]
                BagSizeUlTB.Text = result[0];

                for (i = 0; i < 3; i++)  // wait for vial lines
                {
                    line = sr.ReadLine();
                    result = line.Split(' ');   
                }

                // read vials
                for (i = 1; i <= 6; i++)
                {
                    line = sr.ReadLine();
                    result = line.Split(' ');          // vial = result[0], sizeOfVial = result[1], volume -= result[2]
                    //for (j = 1; result[j] == ""; j++)  // read each substring (skipping the first, the number)
                    //{ }                                // skipping the spaces to find the parameter at location j
                    vial = result[0];
                    sizeOfVial = result[1];
                    volume = result[2];

                    strSize = $"Vial{i:D1}SizeUlTB";                           // 1 2 3 volume column
                    strWithraw = $"Vial{i:D1}WithdrawTB";                           // 1 2 3 volume column
                    // now insert into table
                    foreach (Control c in RunParametersTLP.Controls)         // find the next ADD
                    {
                        if (c is TextBox && string.Equals(strSize, c.Name))    // is volume?
                        {
                            c.Text = result[1];
                        }
                        if (c is TextBox && string.Equals(strWithraw, c.Name))    // is volume?
                        {
                            c.Text = result[2];
                        }
                    }
                }

                // read vibration
                for (i = 0; i < 2; i++)  // wait for vibration lines
                {
                    line = sr.ReadLine();
                    result = line.Split(' '); //
                }
                line = sr.ReadLine();
                result = line.Split(' ');  // time of 4 = result[0], time of 5 = result[1], HZ = result[2], strength = result[3]
                vibrationTime4TB.Text = result[0];
                vibrationTime56TB.Text = result[1];
                vibrationHzTB.Text = result[2];
                vibrationStrengthTB.Text = result[3];

                sr.Close();

                //str = new String(c.Name.ToCharArray(0, 3));       // get the first 3 characters from name
                //if (c is TextBox && string.Equals(str, "add"))    // is "add.."?
                //{
                //    currentvial = Convert.ToUInt32(new String(c.Name.ToCharArray(3, 2)));   // get the vial number from name
                //    if (currentvial == i)
                //    {
                //        this.Invoke((MethodInvoker)delegate { c.Text = volume; });
                //        break;
                //    }
                //}
            }
        }

        // ===========================================================================================
        //                                   R U N   button
        // ===========================================================================================
        //
        // =====================================================================================================
        private void RunBtn_Click(object sender, EventArgs e)    // clicking run will start a thread for run
                                                                 // =====================================================================================================
        {
            //if (!readyForNewCommand)
            //{
            //    logAndShow("The robot is busy. Check if the tips are empty.");
            //    return;
            //}
            //if (RunInProcess)
            //{
            //    logAndShow("The PC did not finish the RUN");
            //    return;
            //}

            // open run file
            //runDay = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
            //cmFile = cmRUNpath + runDay + cmFileNameEnd;      // this is the run file name
            //string toWrite = " RescueDose CM bag RUN report\n"
            //               + "\n Date: " + runDay
            //               + "\n user: " + username + "\n\n"
            //               + "====================================================\n"
            //               + "       Vial:                     Vibrate \n"
            //               + " size [ml]  withdraw [ml]     HZ [1/s]  strength[%]\n"
            //               + "====================================================\n\n";
            //File.WriteAllText(cmPath + cmFile, toWrite);

            //WriteCMrun();      // *****  create file for future loading   ****
            stopOnError = false;  // ok to run cycle
            aborted = false;

            Thread runThread = new Thread(RunBtn_Click_Impl);     // start the thread for "run"
            runThread.Start();                                    // runThread.Start();
        }
        // ===========================================================================================
        //                                   R U N   Thread
        // ===========================================================================================
        //
        // this is the method to run the bag filling according to the textBox table
        // the following thread will run the RUN button request
        private void RunBtn_Click_Impl()
        {
            //uint test;
            //uint bitBottle;
            //uint chosenBit = 0;
            //uint toDo = 0b111111111111111111;
            //uint addValue;
            //int factorValue;
            //uint largestAdd;
            //uint smalestAdd;
            //uint largestBottle = 1;
            //uint smalestBottle = 1;
            //uint currentBottle = 0;
            //string str = "";
            ////string noData = " in process";
            //string totalSoFar = "";
            Control dd = new Control();
            Control ee = new Control();
            // ==============================
            // check data in the table boxes
            // ==============================

            RunInProcess = true;                                  // eliminate re-entrance

            // ************* goHome first ******************************

            tResponse = rTMCConn.RunCommand(GeneralFunctions.FIND_HOMES);
            tstringToRUNtest();
            Thread.Sleep(300);
            //while (!homingDone)  // wait for the end of "HOME"
            //{
            //}
            //while (!readyForNewCommand)  // wait for the end of "Calibrate"
            //{
            //}
            //this.Invoke((MethodInvoker)delegate { RunParametersTLP.Enabled = false; });

            //if (!readyForNewCommand)
            //{
            //    logAndShow("The Robot is busy, wait and try again");
            //    return;
            //}
            if (rTMCConn.TrinamicAborted())
            {
                logAndShow(" returning from RUN thread");
                goto exit;  // exit
            }

            //  ************ send RUN parameters to board *******************


            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_1, BagSizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_1, Vial1SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_2, Vial2SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_3, Vial3SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_4, Vial4SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_5, Vial5SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vialSize_uL_6, Vial6SizeUlTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_1, Vial1WithdrawTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_2, Vial2WithdrawTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_3, Vial3WithdrawTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_4, Vial4WithdrawTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_5, Vial5WithdrawTB.Text);
            //tstringToSGPtest();
            tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_microLtoWithdraw_6, Vial6WithdrawTB.Text);
            tstringToSGPtest();



            tResponse = rTMCConn.RunCommand(GeneralFunctions.DRAW_DOSE);   // run one bottle filling, with multiple tips rounds
            tstringToRUNtest();

            Thread.Sleep(300);  // wait before polling the "ready for new command

            //    // get volume control
            //    str = $"volume{currentBottle:D2}tb";            // D2 to get 01 02 03...
            //    foreach (Control d in RunParametersTLP.Controls)   // find the control of current bottle
            //    {
            //        if (d is TextBox && string.Equals(str, d.Name))
            //        {
            //            dd = d;                           // copy to a control out of "foreach"
            //            this.Invoke((MethodInvoker)delegate { dd.BackColor = Color.PaleTurquoise; });
            //            this.Invoke((MethodInvoker)delegate { dd.Font = new Font(textBox1.Font, FontStyle.Regular); });
            //            break;
            //        }
            //    }

            //    // get invetory  control
            //    str = $"RX{currentBottle:D2}tb";                // D2 to get 01 02 03...
            //    foreach (Control e in RunParametersTLP.Controls)   // find the control of current bottle
            //    {
            //        if (e is TextBox && string.Equals(str, e.Name))
            //        {
            //            ee = e;                           // copy to a control out of "foreach"
            //            break;
            //        }
            //    }
            //    if (dd.Text == "")
            //    {
            //        this.Invoke((MethodInvoker)delegate { dd.Text = "0"; });  // print "0" on volume
            //    }
            //    volumeDone[currentBottle] = Convert.ToInt32(dd.Text);
            //    while (!readyForNewCommand)            // wait for the end of "MULTI"
            //    {
            //        if (stopOnError || aborted) // Stop looping
            //        {
            //            // ErrorsLog();
            //            // print the volume so far before the error
            //            // this.Invoke((MethodInvoker)delegate { dd.Text = totalSoFar; });
            //            this.Invoke((MethodInvoker)delegate { dd.BackColor = Color.SeaShell; });
            //            this.Invoke((MethodInvoker)delegate { dd.Font = new Font(textBox1.Font, FontStyle.Regular); });
            //            goto exit;  // exit
            //        }
            //        if (microLdispensedSoFar != 0)     // good data data
            //        {
            //            double v1 = microLdispensedSoFar * 100.0 / factorValue + volumeDone[currentBottle];
            //            totalSoFar = Convert.ToString(Math.Ceiling(v1));
            //            if (totalSoFar != dd.Text)
            //            {
            //                // runDay = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
            //                // LUfile = LUpath + runDay + LUfileNameEnd;      // this is the run file name
            //                File.AppendAllText(isotopiaPath + LUfile, $" {ee.Text,12} {totalSoFar,24}\n");
            //                CreatePdfFile(LUfile);
            //            }
            //            this.Invoke((MethodInvoker)delegate { dd.Text = totalSoFar; });
            //        }
            //        else      // if = 0, no data yet
            //        {
            //            this.Invoke((MethodInvoker)delegate { dd.BackColor = Color.LawnGreen; }); // dd.Text = noData; });
            //            this.Invoke((MethodInvoker)delegate { dd.Font = new Font(textBox1.Font, FontStyle.Bold); });
            //        }
            //    }
            //    this.Invoke((MethodInvoker)delegate { dd.BackColor = Color.SeaShell; });
            //    this.Invoke((MethodInvoker)delegate { dd.Font = new Font(textBox1.Font, FontStyle.Regular); });

            //    // a bottle is done, add info to the report file 
            //    writeLogFile($" bottle {currentBottle:D2}  inventory  \"{ee.Text}\"  is done with {totalSoFar}");

            //    //                if (dd.Text == "")
            //    //                {
            //    //                    this.Invoke((MethodInvoker)delegate { dd.Text = "0"; });  // print "0" on volume
            //    //                }
            //    if (rgNumber.Match(dd.Text).Success)        // match, a number character is there
            //    {
            //        volumeDone[currentBottle] = Convert.ToInt32(dd.Text);
            //    }

            //    // goto the next bottle
            //    bitBottle = Convert.ToUInt32(Math.Pow(2, currentBottle - 1));
            //    toDo &= ~bitBottle;   // remove bit, done               

            // all bottles are done

            //File.AppendAllText(cmPath + cmFile, $" {"\n The RUN process ended at:"} {DateTime.Now.ToString("yyyy-MM-dd HH-mm")} \n");
            //tResponse = rTMCConn.RunCommand(GeneralFunctions.MOVE_DROP_POSITION_TIP);
            //tstringToRUNtest();

            Thread.Sleep(300);           // wait before polling the "ready for new command
                                         //while (!readyForNewCommand)  // wait for the end of "MOVE_DROP_POSITION_TIP"
                                         //{
                                         //}
            logAndShow("The RUN process is done");
            WriteCMrun();      // *****  create RUN results file   ****
        exit:
            //this.Invoke((MethodInvoker)delegate { RunParametersTLP.Enabled = true; });
            RunInProcess = false;

        }
        // =============================================================================
        // =============================================================================
        //private void clrADDbtn_Click(object sender, EventArgs e)
        //{
        //    if (RunInProcess || !readyForNewCommand)
        //    {
        //        logAndShow("The robot is busy");
        //        return;
        //    }
        //    string str;
        //    foreach (Control c in RunParametersTLP.Controls)         // look for smalest and largest "add" value
        //    {
        //        str = new String(c.Name.ToCharArray(0, 3));       // get the first 3 characters from name
        //        if (c is TextBox && string.Equals(str, "add"))    // is "add.."?
        //        {
        //            c.Text = "";                                  // clear all of them
        //        }
        //    }
        //}
        // ===================================
        // **** m o t o r s   c o n t r o l
        // ===================================

        // *** Vertical motor control ***
        private void VerticalUpBtn_Click(object sender, EventArgs e)
        {
            VerticalGoUp();
        }

        private void VerticalUpArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            VerticalGoUp();
        }

        private void VerticalDownArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            VerticalGoDown();
        }

        private void VerticalDownBtn_Click(object sender, EventArgs e)
        {
            VerticalGoDown();
        }

        public void VerticalGoUp()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.VerticalManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }
        public void VerticalGoDown()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.VerticalManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Linear motor control ***
        private void LinearLeftBtn_Click(object sender, EventArgs e)
        {
            LinearGoLeft();
        }

        private void LinearRightBtn_Click(object sender, EventArgs e)
        {
            LinearGoRight();
        }

        private void LinearLeftArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            LinearGoLeft();
        }

        private void LinearRightArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            LinearGoRight();
        }

        public void LinearGoLeft()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.LinearMotorManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }
        public void LinearGoRight()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.LinearMotorManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Arm motor control ***

        private void ArmDownBtn_Click(object sender, EventArgs e)
        {
            ArmGoDown();
        }

        private void ArmUpBtn_Click(object sender, EventArgs e)
        {
            ArmGoUp();
        }

        private void ArmDownArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            ArmGoDown();
        }

        private void ArmUpArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            ArmGoUp();
        }

        public void ArmGoUp()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward = "-1"
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.armMotorManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        public void ArmGoDown()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // Forward = "1"
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.armMotorManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Piston motor control ***
        private void PistonInBtn_Click(object sender, EventArgs e)
        {
            PistonGoIn();
        }

        private void PistonOutBtn_Click(object sender, EventArgs e)
        {
            PistonGoOut();
        }

        private void PistonInArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            PistonGoIn();
        }

        private void PistonOutArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            PistonGoOut();
        }

        public void PistonGoIn()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.PistonManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }
        public void PistonGoOut()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // Forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.PistonManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Head rotate motor control ***
        private void SyringeUpBtn_Click(object sender, EventArgs e)
        {
            HeadRotateUp();
        }

        private void SyringeDownBtn_Click(object sender, EventArgs e)
        {
            HeadRotateDown();
        }

        private void SyringeUpArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            HeadRotateUp();
        }

        private void SyringeDownArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            HeadRotateDown();
        }

        public void HeadRotateUp()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }
        public void HeadRotateDown()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // Forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Dispose motor control ***
        private void DisposeBtn_Click(object sender, EventArgs e)
        {
            DisposeGoOut();
        }

        private void DiposeBackBtn_Click(object sender, EventArgs e)
        {
            DisposeGoIn();
        }

        private void DisposeArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            DisposeGoOut();
        }

        private void DisposeBackArrowPnl_Paint(object sender, PaintEventArgs e)
        {
            DisposeGoIn();
        }

        public void DisposeGoOut()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }
        public void DisposeGoIn()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // Forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // *** Cap Holder motor control ***
        private void HoldCapBtn_Click(object sender, EventArgs e)
        {
            CapHolderHold();
        }

        private void ReleaseCapBtn_Click(object sender, EventArgs e)
        {

        }

        private void CapHoldArowPnl_Paint(object sender, PaintEventArgs e)
        {
            CapHolderHold();
        }

        private void CapReleaseArowPnl_Paint(object sender, PaintEventArgs e)
        {
            CapHolderRelease();
        }

        public void CapHolderHold()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Backward);   // Backward=-1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        public void CapHolderRelease()
        {
            if (readyForNewCommand)
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_moveManualBackwards, Values.Forward);   // Forward=1
                tstringToSGPtest();   // display on "for SGP cmd"

                tResponse = rTMCConn.RunCommand(GeneralFunctions.RotationManual);
                tstringToRUNtest();    // display on "for RUN cmd"
            }
        }

        // ******************
        //  set jog distance
        // ******************

        private void Jog20RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "20000";
            setManualDistance();
        }

        private void Jog5RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "5000";
            setManualDistance();
        }

        private void Jog2RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "2000";
            setManualDistance();
        }

        private void Jog1RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "1000";
            setManualDistance();
        }

        private void Jog04RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "400";
            setManualDistance();
        }

        private void Jog02RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "200";
            setManualDistance();
        }

        private void Jog01RB_CheckedChanged(object sender, EventArgs e)
        {
            goDistanceTB.Text = "100";
            setManualDistance();
        }
        // ************************
        // ** set distance to go **
        // ************************

        private void goDistanceTB_Leave(object sender, EventArgs e)
        {
            setManualDistance();
        }

        private void goDistanceTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setManualDistance();
            }
        }
        private void setManualDistance()
        {
            if (rgNumber.Match(goDistanceTB.Text).Success)        // did not match, a non number character is there
            {
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_UnitsToMoveManual, goDistanceTB.Text);
            }
            refreshParams();
        }

        // ***********
        //  GOTO HOME
        // ***********

        private void RunHomeBtn_Click(object sender, EventArgs e)
        {
            goHome();
        }
        private void calibrateHOMEbtn_Click(object sender, EventArgs e)
        {
            goHome();
        }
        public void goHome()
        {
            Thread goHomeThread = new Thread(goHome_Impl);        // start the thread for "run"
            goHomeThread.Start();
        }
        private void goHome_Impl()
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.INIT_CM);
            tstringToRUNtest();    // display on "for RUN cmd"
            Thread.Sleep(300);
            ////while (!homingDone)  // wait for the end of "HOME"
            ////{
            ////}
            Thread.Sleep(300);           // wait before polling the "ready for new command
            logAndShow("Go home done.");
            aborted = false;
            RunInProcess = false;
        }

        private void CalibrateAbortBtn_Click(object sender, EventArgs e)
        {
            AbortCM();
        }
        private void RunAbortBtn_Click(object sender, EventArgs e)
        {
            AbortCM();
        }
        private void AbortCM()
        {
            aborted = true;
            tResponse = rTMCConn.RunCommand(GeneralFunctions.ABORT);
            tstringToRUNtest();    // display on "for RUN cmd"
            Thread.Sleep(300);           // wait before polling the "ready for new command
            tResponse = rTMCConn.RunCommand(GeneralFunctions.FIRST_RUN);
            tstringToRUNtest();    // display on "for RUN cmd"
            logAndShow("Aborted. Run HOME");
            // Environment.Exit(0);
        }

        private void cmTC_KeyDown(object sender, KeyEventArgs e)
        {
            /*MessageBox.Show(
                        "" 
                        + " KeyCode =  " + e.KeyCode.ToString()
                        + "\r value=   " + e.KeyValue
                        + "\r control= " + e.Control
                        + "\r Alt=     " + e.Alt);
            */
            if (username == "") { return; }
            if (isAdministrator == false) { return; }
            if (currentTAB <= 2) { return; }             // only for calibrate and dispense

            switch (Convert.ToInt32(e.KeyCode))      // move motors using keyboard
            {
                case (char)221: VerticalGoUp(); break;      // "]"
                                                            //case (char)39: VerticalGoUp(); break;      // "->"
                case (char)219: VerticalGoDown(); break;       // "["
                                                               //case (char)37: VerticalGoDown(); break;       // "<-"
                case (char)222: LinearGoRight(); break;    // '"'
                                                           //case (char)40: LinearGoRight(); break;    // 'down'
                case (char)187: LinearGoLeft(); break;       // "+"
                                                             //case (char)38: LinearGoLeft(); break;       // "up"
                case (char)109: ArmGoUp(); break;     // "-" on key pad
                case (char)107: ArmGoDown(); break;       // "+" on key pad
                case (char)33: HeadRotateUp(); break;      // "page Up"
                case (char)34: HeadRotateDown(); break;    // "page down"
                case (char)36: PistonGoIn(); break;       // "Home"
                case (char)35: PistonGoOut(); break;     // "End"
            }
        }
        // ***** set the tabs entered a number *****
        private void adminTP_Enter(object sender, EventArgs e)
        {
            currentTAB = 1;
        }
        private void RunTP_Enter(object sender, EventArgs e)
        {
            currentTAB = 2;
            refreshParams();
        }
        private void calibrateTP_Enter(object sender, EventArgs e)
        {
            currentTAB = 3;
            refreshParams();
        }
        private void SetupsTP_Enter(object sender, EventArgs e)
        {
            currentTAB = 4;
            refreshParams();
        }

        // ==============
        //    BackUp
        // ==============
        private void AdmBackUpBtn_Click(object sender, EventArgs e)
        {
            int i;
            string toDay;
            string fileName;

            string fileNameEnd = " Backup sn " + robotSerialTB.Text + " sw " + TrinamicCodeTB.Text + ".txt";
            // ==============================
            // save file
            toDay = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
            fileName = cmPath + backupPath + toDay + fileNameEnd;
            File.WriteAllText(fileName, "\n Backup of Isotopia sn: " + robotSerialTB.Text + "  sw: " + TrinamicCodeTB.Text + "\n" +
                                         "\n Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") +
                                         "\n User: " + username + "\n\n" +
                                         "=============== \n" +
                                         " GP        value\n" +                  // append to the file
                                         "=============== \n\n");
            for (i = 0; i < 56; i++)
            {
                tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, i.ToString("D"));
                tstringToSGPtest();   // display on "for SGP cmd"
                File.AppendAllText(fileName, i.ToString("D3") + $"{SGPtestTB.Text,12}\n");
            }
            //CreatePdfFile(backupPath + toDay + fileNameEnd);
            logAndShow("Trinamic board back up done: " + fileNameEnd);
        }

        // ==============
        //    RESTORE
        // ==============
        private void AdmRestoreBtn_Click(object sender, EventArgs e)
        {
            int i;
            int j;
            string line;
            string GP;
            string value;
            string[] result; // = new string[16];

            openFileDialog1.InitialDirectory = cmPath + backupPath;
            openFileDialog1.Filter = "text files (*.txt)|*.txt";
            DialogResult dr = openFileDialog1.ShowDialog();  // choose the directory from file list 
            if (dr == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                for (i = 0; i < 10; i++)  // wait for first lines
                {
                    sr.ReadLine();
                }
                for (i = 0; i < 56; i++)
                {
                    line = sr.ReadLine();
                    result = line.Split(' ');
                    for (j = 1; result[j] == ""; j++)  // read each substring (skipping the first, the number)
                    { }                                // skipping the spaces to find the parameter at location j
                    GP = result[0];
                    value = result[j];
                    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, GP, value);
                    tstringToSGPtest();   // display on "for SGP cmd"
                }
                logAndShow($"Trinamic parameters restored to the robot: \'{openFileDialog1.FileName}\'");
                sr.Close();
            }
        }

        // ***********************
        //       PASSWORD
        // ***********************

        private void visibleUser()
        {
            isAdministrator = false;
            debugPnl.Visible = true;
            GBpanelPNL.Enabled = false;
            backupPnl.Visible = false;
            addRemovePnl.Visible = false;
            PWfileEmptyPnl.Visible = false;
            userPWtlp.Visible = true;
            logoutBtn.Visible = true;
            addToLogPnl.Visible = true;

            UserAdminTLP.Visible = true;
            RunSC.Visible = true;
            calibrateTLP.Visible = false;
            setupTLP.Visible = false;
        }
        private void visibleMaster()
        {
            isAdministrator = true;
            FilesPathTB.Text = cmPath;
            maxBadPWtb.Text = Convert.ToString(maxPWtrials);
            monthsForPWtb.Text = Convert.ToString(maxPWmonths);
            percAddP100tb.Text = Convert.ToString(percAddP100);

            debugPnl.Visible = true;
            GBpanelPNL.Enabled = true;
            backupPnl.Visible = true;
            addRemovePnl.Visible = true;
            PWfileEmptyPnl.Visible = false;
            userPWtlp.Visible = true;
            logoutBtn.Visible = true;
            addToLogPnl.Visible = true;

            UserAdminTLP.Visible = true;
            RunSC.Visible = true;
            calibrateTLP.Visible = true;
            setupTLP.Visible = true;
        }
        private void visibleFalse()
        {
            isAdministrator = false;
            debugPnl.Visible = false;
            GBpanelPNL.Enabled = false;
            backupPnl.Visible = false;
            addRemovePnl.Visible = false;
            PWfileEmptyPnl.Visible = false;
            userPWtlp.Visible = true;
            logoutBtn.Visible = false;
            addToLogPnl.Visible = false;

            UserAdminTLP.Visible = true;
            RunSC.Visible = false;
            //RunParametersTLP.Visible = false;
            calibrateTLP.Visible = false;
            setupTLP.Visible = false;
        }

        // =================
        //    check pw
        // =================
        private void enterPWbtn_Click(object sender, EventArgs e)
        {
            string newHashString;
            string line;
            string[] result;
            DateTime stopPwDate;
            DateTime gracePwDate;
            TimeSpan overDuePWdays;
            TimeSpan gracePWdays;

            username = userNameTB.Text;
            userPassWord = userPWtb.Text;
            userPWtb.Text = "";

            if (username == "")
            {
                visibleFalse();
                JustShow("user name is missing");
            }
            else if (userPassWord == "")
            {
                visibleFalse();
                JustShow("Password is missing");
            }
            else  // both ok
            {
                if (!File.Exists(cmPath + pwPath + pwFileName))     // password file exists?
                {
                    logAndShow("PW file does not exist. Try master PW");
                    visibleFalse();
                    PWfileEmptyPnl.Visible = true;          // enable master PW entrance, PW = DATE (2020-12-06)
                    userPWtlp.Visible = false;
                    return;
                }
                // open PW file:
                StreamReader sr = new StreamReader(cmPath + pwPath + pwFileName);

                line = sr.ReadLine();  // read first line
                if (line == null)
                {
                    logAndShow("PW file is empty \n Try master PW");
                    visibleFalse();
                    PWfileEmptyPnl.Visible = true;
                    userPWtlp.Visible = false;
                    return;
                }
                newHashString = getHashString(userPassWord);      // now calculate the HASH string

                while (line != null)
                {
                    result = line.Split(';');
                    if (result[0] == username)             // found the user
                    {
                        if (result[1] == "yesAdmin")
                        {
                            isAdministrator = true;
                        }
                        if (Convert.ToInt32(result[3]) >= maxPWtrials)   // check number of missed PW trials
                        {
                            if (isAdministrator)
                            {
                                logAndShow("Exeeded max PW tries \n Try master PW");
                                visibleUser();
                                PWfileEmptyPnl.Visible = true;
                                //userPWtl.Visible = false;
                            }
                            else //  !isAdministrator
                            {
                                logAndShow($"Exeeded max PW tries. Erase and create new user \'{username}\'");
                                sr.Close();
                                leftTries = incFailedTimes(username);
                            }
                            return;
                        }
                        // check PW creation time overdue

                        stopPwDate = Convert.ToDateTime(result[2]).AddMonths(maxPWmonths);
                        overDuePWdays = DateTime.Now - stopPwDate;
                        if (overDuePWdays.Days > 0 && !isAdministrator)   // it means we passed the limit
                        {
                            gracePwDate = stopPwDate.AddDays(maxGracePWdays);
                            gracePWdays = DateTime.Now - gracePwDate;
                            if (gracePWdays.Days > 0)   // it means we passed the grace time
                            {
                                logAndShow("PW over due. Goto administrator to renew your password");
                                sr.Close();
                                return;
                            }
                            logAndShow($"Exeeded max PW renewal by {overDuePWdays.Days} days \n" +
                                            $"            You have {-gracePWdays.Days} grace days left");
                        }
                        if (result[4] == newHashString)    // check the hashed PW
                        {
                            if (result.Length == 5)
                            {
                                if (result[1] == "yesAdmin")
                                {
                                    isAdministrator = true;
                                    visibleMaster();
                                }
                                else
                                {
                                    isAdministrator = false;
                                    visibleUser();
                                }
                            }
                            else
                            {
                                isAdministrator = false;
                                visibleUser();
                            }
                            sr.Close();
                            resetFailedTimes(username);
                            logAndShow("Welcome " + (isAdministrator ? "Admin " : "") + "user \'" + username + " \'");
                            return;
                        }
                        else
                        {
                            sr.Close();
                            leftTries = incFailedTimes(username);
                            logAndShow($"PW not correct. You have {leftTries} more tries");
                            return;
                        }
                    }
                    line = sr.ReadLine();
                }
                logAndShow("no maching user was found. Try logging again.");
                sr.Close();
            }
        }
        // *****************
        // new user write
        // *****************
        private void newUserBtn_Click(object sender, EventArgs e)
        {
            userPassWord = newUserPwTB.Text;
            newUserPwTB.Text = "";
            bool needToEraseUser = false;
            bool erasedSuccessfully;
            string line;
            string[] result;
            string newHashString;
            bool Finished = false;

            while (!Finished)
            {
                if (newUserNameTB.Text == "")
                {
                    JustShow("Please enter user name ");
                    break;
                }
                else if (userPassWord == "")   //pwCurrent)
                {
                    // check PW validity
                    JustShow("Please enter Password ");
                    break;
                }
                else if (userPassWord.Length < 4)
                {
                    JustShow("Please enter Password with at least 4 characters");
                    break;
                }
                else  // filled -> set a hash and save to file
                {
                    newHashString = getHashString(userPassWord);      // now calculate the HASH string

                    //  if the file already exists, check for user already exist

                    if (File.Exists(cmPath + pwPath + pwFileName))
                    {
                        StreamReader sr = new StreamReader(cmPath + pwPath + pwFileName);
                        sr.ReadLine();  // read first info line
                        sr.ReadLine();  // read second "===" line
                        line = sr.ReadLine();  // read third 1'st data line

                        while (line != null)
                        {
                            result = line.Split(';');
                            if (result[0] == newUserNameTB.Text)             // found the user?
                            {
                                DialogResult dr1 = MessageBox.Show($"user \'{newUserNameTB.Text}\' already exists \n" +
                                                                   $"Do you want to overwrite \'{newUserNameTB.Text}\' ?",
                                                                   "overwrite existing user?", MessageBoxButtons.YesNo);
                                if (dr1 == DialogResult.No)
                                {
                                    sr.Close();
                                    return;
                                }
                                else //  if (dr1 == DialogResult.Yes)
                                {
                                    sr.Close();
                                    needToEraseUser = true;
                                    break;     // over write the user
                                }
                            }
                            line = sr.ReadLine();
                        }
                        sr.Close();
                    }
                    else //file does not exists
                    {
                        File.WriteAllText(cmPath + pwPath + pwFileName, "** user; yesAdmin; dateCreated; times PW failed; PW hash \n"
                                                                + "** ==================================================== \n");
                    }
                    if (needToEraseUser)
                    {
                        erasedSuccessfully = eraseUser(newUserNameTB.Text);
                        if (!erasedSuccessfully)
                        {
                            logAndShow("Failed to erase user \'" + newUserNameTB.Text + "\'");
                            return;
                        }
                    }

                    // no user duplication, add the new user

                    File.AppendAllText(cmPath + pwPath + pwFileName, newUserNameTB.Text);    // write user name
                    if (IsAdminCkb.Checked)
                    {
                        File.AppendAllText(cmPath + pwPath + pwFileName, ";yesAdmin");        // assign administrator
                    }
                    else
                    {
                        File.AppendAllText(cmPath + pwPath + pwFileName, ";notAdmin");       // assign administrator
                    }
                    File.AppendAllText(cmPath + pwPath + pwFileName, DateTime.Now.ToString(";yyyy-MM-dd")  // Creation date
                                                            + ";0"                                  // times PW failed
                                                            + $";{newHashString}"                   // write hashed PW
                                                            + "\n");                                // new line

                    //CreatePdfFile(pwPath + pwFileName);
                    IsAdminCkb.Checked = false;
                    Finished = true;
                    logAndShow("new " + (IsAdminCkb.Checked ? "Admin " : "") + "user \'" + newUserNameTB.Text + "\' added");
                }
            }
        }
        // =================
        //     master pw
        // =================
        private void masterPWtb_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                username = "master";
                userPassWord = masterPWtb.Text;

                if (userPassWord == "")
                {
                    visibleFalse();
                    PWfileEmptyPnl.Visible = true;
                    logAndShow("Please Login first with admin PassWord");
                }
                else  // ok, not empty
                {
                    pwMaster = DateTime.Now.ToString("yyyy-MM-dd");
                    //pwMaster = $"RD-{CyclesTotalTB.Text}";

                    if (userPassWord != pwMaster)
                    {
                        visibleFalse();
                        PWfileEmptyPnl.Visible = true;
                        logAndShow("wrong PassWord, try again");
                    }
                    else  // master OK
                    {
                        visibleMaster();
                        PWfileEmptyPnl.Visible = false;
                        logAndShow("Welcome master user " + username);
                    }
                    masterPWtb.Text = "";
                }
            }
        }
        // =================
        //   hash creator
        // =================
        private string getHashString(string pwString)
        {
            string hashString = "";
            byte[] PWhashValue;
            UnicodeEncoding ue = new UnicodeEncoding();      // Create a new instance of the UnicodeEncoding class to
            byte[] messageBytes = ue.GetBytes(pwString);     // Convert the string into an array of Unicode bytes.
            SHA256 shHash = SHA256.Create();                 // Create a new instance of the SHA256 class to create the hash value.
            PWhashValue = shHash.ComputeHash(messageBytes);  // Create the hash value from the array of bytes.
            for (int x = 0; x < PWhashValue.Length; x++)
            {
                hashString += PWhashValue[x];
            }
            return hashString;
        }

        // ==========================
        //   erase user from button
        // ==========================
        private void eraseUserBtn_Click(object sender, EventArgs e)
        {
            bool erasedSuccessfully;

            DialogResult dr1 = MessageBox.Show($"please confirm erasing user: \'{eraseUserTB.Text}\' \n" +
                                               $"Do you want to erase \'{eraseUserTB.Text}\' ?",
                                               "erase existing user?", MessageBoxButtons.YesNo);
            if (dr1 == DialogResult.No)
            {
                return;
            }
            else //  if (dr1 == DialogResult.Yes)
            {
                erasedSuccessfully = eraseUser(eraseUserTB.Text);
                if (!erasedSuccessfully)
                {
                    logAndShow($"Failed to erase user \'{eraseUserTB.Text}\'");
                    return;
                }
            }
        }
        // ==============
        //   erase user
        // ==============
        private bool eraseUser(string user)
        {
            string[] result;
            int i, j;
            try
            {
                // Open the file to read from.
                string[] readPWs = File.ReadAllLines(cmPath + pwPath + pwFileName);
                for (i = 2; i < readPWs.Length; i++)
                {
                    result = readPWs[i].Split(';');
                    if (result[0] == user)             // found the user -> erase line
                    {
                        for (j = i; j < readPWs.Length - 1; j++)
                        {
                            readPWs[j] = readPWs[j + 1];
                        }
                        Array.Resize(ref readPWs, readPWs.Length - 1); ;
                        File.WriteAllLines(cmPath + pwPath + pwFileName, readPWs);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                logAndShow($"Failed to open file \'{cmPath + pwPath + pwFileName}\'\n {e.Message}");
                return false;
            }
            logAndShow($"Erased user \'{user}\'");
            return true;
        }
        // ===============================
        //   increment times of wrong PW
        // ===============================
        private Int32 incFailedTimes(string user)
        {
            string[] result;
            int i;
            int timesFailed = 0;
            try
            {
                // Open the file to read from.
                string[] readPWs = File.ReadAllLines(cmPath + pwPath + pwFileName);
                for (i = 0; i < readPWs.Length; i++)
                {
                    result = readPWs[i].Split(';');
                    if (result[0] == user)             // found the user -> erase line
                    {
                        timesFailed = Convert.ToInt32(result[3]) + 1;
                        result[3] = Convert.ToString(timesFailed); // increment                        
                        readPWs[i] = String.Join(";", result);
                        File.WriteAllLines(cmPath + pwPath + pwFileName, readPWs);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                logAndShow($"failed to edit PW file \n{e.Message}");
            }
            return maxPWtrials - timesFailed;  // return left trials
        }
        // ==================================
        //   reset Failed Times of wrong PW
        // ==================================
        private bool resetFailedTimes(string user)
        {
            string[] result;
            int i;
            try
            {
                // Open the file to read from.
                string[] readPWs = File.ReadAllLines(cmPath + pwPath + pwFileName);
                for (i = 0; i < readPWs.Length; i++)
                {
                    result = readPWs[i].Split(';');
                    if (result[0] == user)               // found the user -> erase line
                    {
                        result[3] = ""; // increment    // clear failed times               
                        readPWs[i] = String.Join(";", result);
                        File.WriteAllLines(cmPath + pwPath + pwFileName, readPWs);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                logAndShow($"failed to edit PW file \n{e.Message}");
            }
            return true;
        }
        // ======================
        //   create params file
        // ======================
        private void changeParamsBtn_Click(object sender, EventArgs e)
        {
            if (FilesPathTB.Text == "" || maxBadPWtb.Text == "" || monthsForPWtb.Text == "" || percAddP100tb.Text == "")
            {
                logAndShow("Please fill all text boxes");
                FilesPathTB.Text = cmPath;
                maxBadPWtb.Text = Convert.ToString(maxPWtrials);
                monthsForPWtb.Text = Convert.ToString(maxPWmonths);
                percAddP100tb.Text = Convert.ToString(percAddP100);
                return;
            }
            checkDirectories();
            WriteParamsFile();
            return;
        }
        private void WriteParamsFile()
        {
            File.WriteAllText(cmPath + paramsPath + paramsFileName, " file Location" + "; " + "PW wrong trials" + "; " + "time lap to renew PW" + "; "
                                                           + "percentage add for P100 10-50 uL \n"
                                                       + " ================================================================================ \n"
                                                       + $"{FilesPathTB.Text}"           // Files Location
                                                       + $";{maxBadPWtb.Text}"           // maxBadPWtb bad pw trials
                                                       + $";{monthsForPWtb.Text}"        // time lap to renew PW
                                                       + $";{percAddP100tb.Text}"        // percentage to add for P100 from 10 to 50 uL, gradually
                                                       + "\n");                          // new line

            //CreatePdfFile(paramsPath + paramsFileName);
            readParamsFile();
            logAndShow("new params file " + cmPath + paramsPath + paramsFileName + " created");
            return;
        }
        private void readParamsFile()
        {
            string[] result;
            if (!File.Exists(cmPath + paramsPath + paramsFileName))
            {
                FilesPathTB.Text = cmPath;
                maxBadPWtb.Text = Convert.ToString(maxPWtrials);
                monthsForPWtb.Text = Convert.ToString(maxPWmonths);
                percAddP100tb.Text = Convert.ToString(percAddP100);
                WriteParamsFile();
                return;
            }
            try
            {
                // Open the file to read from.
                string[] readParams = File.ReadAllLines(cmPath + paramsPath + paramsFileName);
                result = readParams[2].Split(';');
                cmPath = result[0];
                maxPWtrials = Convert.ToInt32(result[1]);
                maxPWmonths = Convert.ToInt32(result[2]);
                percAddP100 = Convert.ToInt32(result[3]);
                //writeLogFile($"parameter file {cmPath + paramsPath + paramsFileName} was read");
            }
            catch (Exception e)
            {
                logAndShow($"failed to read params file \n{e.Message}");
            }
            return;
        }
        // ==================
        //   write log file
        // ==================
        private void writeLogFile(string message)
        {
            if (!File.Exists(cmPath + logPath + logFileName))
            {
                FilesPathTB.Text = cmPath;
                File.WriteAllText(cmPath + logPath + logFileName, "** Isotopia Log file \n"
                                                        + "** ==================================================== \n\n");
                // recursion, call this function again after the file was created and report creation
                logAndShow("new log File \'" + cmPath + logPath + logFileName + "\' Created");
            }
            try
            {
                File.AppendAllText(cmPath + logPath + logFileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm    ")  // Creation date
                                                         + $"user: \'{username}\'  "                      // user
                                                         + message + "\n");                               // maessage & new line
            }
            catch (Exception e)
            {
                logAndShow($"{e.Message}");
            }
            //CreatePdfFile(logPath + logFileName);
            return;
        }
        // ===============
        //   just show
        // ===============
        private void JustShow(string message)
        {
            MessageBox.Show(message, "information", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                                         MessageBoxDefaultButton.Button3, MessageBoxOptions.DefaultDesktopOnly);
            //this.TopMost = false; 
            return;
        }
        // ===============
        //   log and show
        // ===============
        private void logAndShow(string message)
        {
            writeLogFile(message);
            //this.TopMost = true;   // to display at the top
            JustShow(message);
            //            MessageBox.Show(message, "information", MessageBoxButtons.OK, MessageBoxIcon.Warning,
            //                                         MessageBoxDefaultButton.Button3, MessageBoxOptions.DefaultDesktopOnly);
            //this.TopMost = false; 
            return;
        }

        // =====================
        //  VIEW & PRINT FILES
        // =====================

        private void logoutBtn_Click(object sender, EventArgs e)
        {
            visibleFalse();
            writeLogFile($"user \'{username} \' logged out");
            username = "";
        }

        private void viewLogBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(cmPath + logPath + logFileName);
            //string pdfFileName = outputFileName(logFileName);
            //System.Diagnostics.Process.Start(cmPath + pdfPath + pdfFileName);    // open file viewer, there I can view & print
        }

        private void viewPwBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(cmPath + pwPath + pwFileName);
            //string pdfFileName = outputFileName(pwFileName);
            //System.Diagnostics.Process.Start(cmPath + pdfPath + pdfFileName);    // open file viewer, there I can view & print
        }

        private void viewParamsBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(cmPath + paramsPath + paramsFileName);
            //string pdfFileName = outputFileName(paramsFileName);
            //System.Diagnostics.Process.Start(cmPath + pdfPath + pdfFileName);    // open file viewer, there I can view & print
        }
        // ********************

        //private void viewRunBtn_Click(object sender, EventArgs e)
        private void CMrunsBtn_Click(object sender, EventArgs e)
        {
            // The button is located at the top right corner of the RUN panel
            openFileDialog1.InitialDirectory = cmPath + cmRUNpath;
            openFileDialog1.Filter = "(*cmRUN*.txt)|*cmRUN*.txt";

            DialogResult dr = openFileDialog1.ShowDialog();  // choose the file from list 
            if (dr == DialogResult.OK)
            {
                curentPrintFile = openFileDialog1.FileName;
                try
                {
                    System.Diagnostics.Process.Start(curentPrintFile);    // open file viewer, there I can view & print
                }
                catch (Exception ex)
                {
                    logAndShow($"{ex.Message}");
                }
            }
        }


        // ====================
        //  check directories
        // ====================
        private void checkDirectories()
        {
            try
            {
                // Determine whether the directories exist.

                if (!Directory.Exists(cmPath)
                    || !Directory.Exists(cmPath + logPath)
                    || !Directory.Exists(cmPath + backupPath)
                    || !Directory.Exists(cmPath + cmRUNpath)
                    || !Directory.Exists(cmPath + paramsPath)
                    || !Directory.Exists(cmPath + pwPath)
                    || !Directory.Exists(cmPath + setupPath)
                   //|| !Directory.Exists(cmPath + pdfPath)
                   )
                {
                    DirectoryInfo di1 = Directory.CreateDirectory(cmPath + logPath);
                    DirectoryInfo di3 = Directory.CreateDirectory(cmPath + backupPath);
                    DirectoryInfo di4 = Directory.CreateDirectory(cmPath + cmRUNpath);
                    DirectoryInfo di5 = Directory.CreateDirectory(cmPath + paramsPath);
                    DirectoryInfo di6 = Directory.CreateDirectory(cmPath + pwPath);
                    DirectoryInfo di7 = Directory.CreateDirectory(cmPath + setupPath);
                    //DirectoryInfo di8 = Directory.CreateDirectory(cmPath + pdfPath);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            finally { }
        }
        private void setRegeditNotepadTextsize80()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"SOFTWARE\Microsoft\Notepad";
            const string keyName = userRoot + "\\" + subkey;

            Registry.SetValue(keyName, "iPointSize", 80);
            //int tInteger = (int)Registry.GetValue(keyName, "iPointSize", -1);
        }

        private void addToLogTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                writeLogFile(addToLogTB.Text);
            }
        }

        private void viewBackupBtn_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = cmPath + cmRUNpath;
            //openFileDialog1.Filter = "(*Backup*.pdf)|*Backup*.pdf";

            DialogResult dr = openFileDialog1.ShowDialog();  // choose the file from list 
            if (dr == DialogResult.OK)
            {
                curentPrintFile = openFileDialog1.FileName;
                try
                {
                    System.Diagnostics.Process.Start(curentPrintFile);    // open file viewer, there I can view & print
                }
                catch (Exception ex)
                {
                    logAndShow($"{ex.Message}");
                }
            }
        }

        // ***********
        //  home axis
        // ***********

        // ** vertical **
        private void VerticalHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homeVerticalMotor);
            tstringToRUNtest();
        }

        private void LinearHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homeLinearMotor);
            tstringToRUNtest();
        }
        private void ArmHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homeArmMotor);
            tstringToRUNtest();
        }

        private void PistonHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homePistonMotor);
            tstringToRUNtest();
        }

        private void HeadRotateHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homeHeadRotateMotor);
            tstringToRUNtest();
        }

        private void DisposeHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.HomeDisposeMotor);
            tstringToRUNtest();
        }

        private void CapHolderHomeBtn_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(GeneralFunctions.homeCapHolderMotor);
            tstringToRUNtest();
        }
        // **************************
        // *** Neddle and  Home setup
        // **************************

        // ** Needle Gauge ***
        private void NeedleGaugeTB_Leave(object sender, EventArgs e)
        {
            needleGaugeSet();
        }

        private void NeedleGaugeTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                needleGaugeSet();
            }
        }
        private void needleGaugeSet()
        {
            if (rgNumber.Match(NeedleGaugeTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {NeedleGaugeTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleGauge, NeedleGaugeTB.Text);
            }
            refreshParams();
        }

        // ** Needle Length ***
        private void NeedleLengthTB_Leave(object sender, EventArgs e)
        {
            NeedleLengthSet();
        }
        private void NeedleLengthTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                NeedleLengthSet();
            }
        }
        private void NeedleLengthSet()
        {
            if (rgNumber.Match(NeedleLengthTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {NeedleLengthTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleLength, NeedleLengthTB.Text);
            }
            refreshParams();
        }

        // ***********************************
        // *** GET / SET global parameters ***
        // ***********************************

        private void setGBnumberTB_TextChanged(object sender, EventArgs e)
        {
            if (!rgNumber.Match(setGBnumberTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the GB {setGBnumberTB.Text}");
                setGBnumberTB.Text = "";
            }
        }

        private void getGBnumberTB_TextChanged(object sender, EventArgs e)
        {
            if (!rgNumber.Match(getGBnumberTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the GB {getGBnumberTB.Text}");
                getGBnumberTB.Text = "";
            }
        }

        private void setGBvalueTB_Leave(object sender, EventArgs e)
        {
            if (!rgMinus.Match(setGBvalueTB.Text).Success)        // did not match, a non number character is there "-" ok
            {
                //logAndShow($"A non-number value for the GB {setGBvalueTB.Text}");
                setGBvalueTB.Text = "";
            }
        }
        private void setGBbtn_Click(object sender, EventArgs e)
        {
            Int32 BGvalue;
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, setGBnumberTB.Text, setGBvalueTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, setGBnumberTB.Text);
            BGvalue = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setGBresultTB.Text = $"{BGvalue}"; });
        }
        // *** Run a command
        private void runCommandTB_Click(object sender, EventArgs e)
        {
            tResponse = rTMCConn.RunCommand(commandToRunTB.Text);
            tstringToRUNtest();    // display on "for RUN cmd"
        }

        // ********************************
        // ** Calibration tab start setups
        // ********************************

        // *** Vertical start ***

        private void setVerticalStartTB_Leave(object sender, EventArgs e)
        {
            setVerticalStart();
        }

        private void setVerticalStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setVerticalStart();
            }
        }

        private void setVerticalStart()
        {
            if (rgMinus.Match(setVerticalStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setVerticalStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos, setVerticalStartTB.Text);
            }
            refreshParams();
        }

        // *** Linear start ***

        private void setLinearStartTB_Leave(object sender, EventArgs e)
        {
            setLinearStart();
        }

        private void setLinearStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setLinearStart();
            }
        }
        private void setLinearStart()
        {
            if (rgMinus.Match(setLinearStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setLinearStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos, setLinearStartTB.Text);
            }
            refreshParams();
        }

        // *** Arm start ***

        private void setArmStartTB_Leave(object sender, EventArgs e)
        {
            setArmStart();
        }

        private void setArmStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setArmStart();
            }
        }

        private void setArmStart()
        {
            if (rgMinus.Match(setArmStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setArmStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition, setArmStartTB.Text);
            }
            refreshParams();
        }

        // *** Piston start ***

        private void setPistonStartTB_Leave(object sender, EventArgs e)
        {
            setPistonStart();
        }

        private void setPistonStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setPistonStart();
            }
        }

        private void setPistonStart()
        {
            if (rgMinus.Match(setPistonStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setPistonStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos, setPistonStartTB.Text);
            }
            refreshParams();
        }


        // *** Head Rotate start ***

        private void setHeadStartTB_Leave(object sender, EventArgs e)
        {
            setHeadStart();
        }

        private void setHeadStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setHeadStart();
            }
        }
        private void setHeadStart()
        {
            if (rgMinus.Match(setHeadStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setHeadStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos, setHeadStartTB.Text);
            }
            refreshParams();
        }


        // *** Dispose start ***

        private void setDisposeStartTB_Leave(object sender, EventArgs e)
        {
            setDisposeStart();
        }

        private void setDisposeStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setDisposeStart();
            }
        }

        private void setDisposeStart()
        {
            if (rgMinus.Match(setDisposeStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setDisposeStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos, setDisposeStartTB.Text);
            }
            refreshParams();
        }

        // *** Cap Holder start ***

        private void setCapStartTB_Leave(object sender, EventArgs e)
        {
            setCapStart();
        }

        private void setCapStartTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setCapStart();
            }
        }

        private void setCapStart()
        {
            if (rgMinus.Match(setCapStartTB.Text).Success)        // did not match, a non number character is there
            {
                //logAndShow($"A non-number value for the distance {setCapStartTB.Text}");
                tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHomePos, setCapStartTB.Text);
            }
            refreshParams();
        }

        // **************************
        // *** RUN TAB set params ***
        // **************************

        // *** set vibration time 4 ***
        private void vibrationTime4TB_Leave(object sender, EventArgs e)
        {
            setVibration4();
        }

        private void vibrationTime4TB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setVibration4();
            }
        }

        private void setVibration4()
        {
            if (rgNumber.Match(microLinVial1TB.Text).Success)        // did not match, a non number character is there
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationTime_4, vibrationTime4TB.Text);
            }
            resreshRUNparameters();
        }

        // *** set vibration time 56 ***
        private void vibrationTime56TB_Leave(object sender, EventArgs e)
        {
            setVibration56();
        }

        private void vibrationTime56TB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setVibration56();
            }
        }

        private void setVibration56()
        {
            if (rgNumber.Match(vibrationTime56TB.Text).Success)        // did not match, a non number character is there
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationTime_56, vibrationTime56TB.Text);
            }
            resreshRUNparameters();
        }

        // *** set vibration HZ ***
        private void vibrationHzTB_Leave(object sender, EventArgs e)
        {
            setVibrationHZ();
        }

        private void vibrationHzTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setVibrationHZ();
            }
        }

        private void setVibrationHZ()
        {
            if (rgNumber.Match(vibrationHzTB.Text).Success
                && Convert.ToInt32(vibrationHzTB.Text) <= 100
                && Convert.ToInt32(vibrationHzTB.Text) >= 4)        // did not match, a non number character is there
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationHz, vibrationHzTB.Text);
            }
            else
            {
                logAndShow("wrong number, or not between 4 and 100");
            }
            resreshRUNparameters();
        }

        // *** set vibration strength ***
        private void vibrationStrengthTB_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Convert.ToInt32(e.KeyCode) == (char)13)    //  Enter key pressed?
            {
                setVibrationStrength();
            }
        }

        private void vibrationStrengthTB_Leave(object sender, EventArgs e)
        {
            setVibrationStrength();
        }

        private void setVibrationStrength()
        {
            if (rgNumber.Match(vibrationStrengthTB.Text).Success
                && Convert.ToInt32(vibrationStrengthTB.Text) <= 100
                && Convert.ToInt32(vibrationStrengthTB.Text) >= 10)        // did not match, a non number character is there
            {
                tResponse = rTMCConn.SetSGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationDutyCyclePercent, vibrationStrengthTB.Text);
            }
            else
            {
                logAndShow("wrong number, or not between 10 and 100");
            }
            resreshRUNparameters();
        }

        // *******************************
        // ** refresh Calibrate Paramters
        // *******************************
        private void refreshParams()
        {
            if (!rTMCConn.TrinamicOK)
            {
                return;
            }
            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos, setVerticalStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos);  //GB_8
            LoadingHight = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setVerticalStartTB.Text = $"{LoadingHight}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos, setLinearStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos); //GB_52
            linearHomePos = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setLinearStartTB.Text = $"{linearHomePos}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition, setArmStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition); // GB_47
            ArmHomePosition = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setArmStartTB.Text = $"{ArmHomePosition}"; });

            // = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos, setPistonStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos); // GB_48
            PistonHomePos = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setPistonStartTB.Text = $"{PistonHomePos}"; });

            // = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos, setHeadStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos); // GB_49
            HeadRotateHomePos = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setHeadStartTB.Text = $"{HeadRotateHomePos}"; });

            // = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos, setDisposeStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos);  // GP_51 not used
            DisposeHomePos = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setDisposeStartTB.Text = $"{DisposeHomePos}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHomePos, setCapStartTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHomePos);  // GP_54 not used
            CapHolderHomePos = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { setCapStartTB.Text = $"{CapHolderHomePos}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleLength, NeedleLengthTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_needleLength);
            NeedleLength = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { NeedleLengthTB.Text = $"{NeedleLength}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleGauge, NeedleGaugeTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_needleGauge);
            NeedleGauge = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { NeedleGaugeTB.Text = $"{NeedleGauge}"; });

            //tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_UnitsToMoveManual, goDistanceTB.Text);
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_UnitsToMoveManual);
            unitsToMove = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { goDistanceTB.Text = $"{unitsToMove}"; });
        }

        // *** refresh RUN TAB parameters  ***
        private void resreshRUNparameters()
        {
            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationTime_4);  // GB_114
            vibrationTime4 = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { vibrationTime4TB.Text = $"{vibrationTime4}"; });

            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationTime_56);  //GB_116
            vibrationTime56 = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { vibrationTime56TB.Text = $"{vibrationTime56}"; });

            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationHz);  //GB_125
            vibrationHz = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { vibrationHzTB.Text = $"{vibrationHz}"; });

            tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_vibrationDutyCyclePercent);  //GB_123
            vibrationStrength = Convert.ToInt32(tResponse.tmcReply.value);
            this.Invoke((MethodInvoker)delegate { vibrationStrengthTB.Text = $"{vibrationStrength}"; });
        }



        // ************
        // ** save all   // not used
        // ************

        //private void setAllParams()
        //{
        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos, setVerticalStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos);  //GB_8
        //    LoadingHight = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setVerticalStartTB.Text = $"{LoadingHight}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos, setLinearStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos); //GB_52
        //    linearHomePos = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setLinearStartTB.Text = $"{linearHomePos}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition, setArmStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition); // GB_47
        //    ArmHomePosition = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setArmStartTB.Text = $"{ArmHomePosition}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos, setPistonStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos); // GB_48
        //    PistonHomePos = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setPistonStartTB.Text = $"{PistonHomePos}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos, setHeadStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos); // GB_49
        //    HeadRotateHomePos = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setHeadStartTB.Text = $"{HeadRotateHomePos}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos, setDisposeStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos);  // GP_51 not used
        //    DisposeHomePos = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setDisposeStartTB.Text = $"{DisposeHomePos}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHomePos, setCapStartTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_CapHolderHomePos);  // GP_54 not used
        //    CapHolderHomePos = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { setCapStartTB.Text = $"{CapHolderHomePos}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleLength, NeedleLengthTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_needleLength);
        //    NeedleLength = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { NeedleLengthTB.Text = $"{NeedleLength}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_needleGauge, NeedleGaugeTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_needleGauge);
        //    NeedleGauge = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { NeedleGaugeTB.Text = $"{NeedleGauge}"; });

        //    tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_UnitsToMoveManual, goDistanceTB.Text);
        //    tResponse = rTMCConn.GetGGP(AddressBank.GetParameterBank, SystemVariables.GB_UnitsToMoveManual);
        //    unitsToMove = Convert.ToInt32(tResponse.tmcReply.value);
        //    this.Invoke((MethodInvoker)delegate { goDistanceTB.Text = $"{unitsToMove}"; });
        //}
        // ***************************
        // *** set start locations ***
        // ***************************

        private void setVerticalBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_VerticalLocationTb.Text) * StepsPerMM.M_VerticalStepsPerMM;
            setVerticalStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_verticalHomePos, setVerticalStartTB.Text);
        }

        private void setLinearBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_LinearLocationTb.Text) * StepsPerMM.M_LinearStepsPerMM;
            setLinearStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_linearHomePos, setLinearStartTB.Text);
        }

        private void setArmBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_ArmLocationTb.Text) * StepsPerMM.M_ArmStepsPerMM;
            setArmStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_ArmHomePosition, setArmStartTB.Text);
        }

        private void setPistonBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_PistonLocationTb.Text) * StepsPerMM.M_PistonStepsPerMM;
            setPistonStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_PistonHomePos, setPistonStartTB.Text);
        }

        private void setHeadRotateBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_headRotateLocationTb.Text) * StepsPerMM.M_RotateStepsPerMM;
            setHeadStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_HeadRotateHomePos, setHeadStartTB.Text);
        }

        private void setDisposeBtn_Click(object sender, EventArgs e)
        {
            v = Convert.ToDouble(M_DisposeLocationTb.Text) * StepsPerMM.M_disposeMicroStepsPerMM;
            setDisposeStartTB.Text = Convert.ToString(Convert.ToInt32(v));
            tResponse = rTMCConn.SetSGPandStore(AddressBank.GetParameterBank, SystemVariables.GB_DisposeHomePos, setDisposeStartTB.Text);
        }

        private void ClrAllBtn_Click(object sender, EventArgs e)
        {
            foreach (Control d in RunParametersTLP.Controls)
            {
                if (d is TextBox)
                {
                    d.Text = "";
                }
            }
        }
    }
}

