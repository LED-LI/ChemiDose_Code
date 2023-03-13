using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceUSB
{
    public static class MotorsNum
    {
        public const string M_Vertical = "0";
        public const string M_Linear = "1";
        public const string M_Arm = "2";
        public const string M_Piston = "3";
        public const string M_HeadRotate = "4";
        public const string M_Dispose = "5";
        public const string M_CapHolder = "6";
    }
    public static class TrinamicInputs
    {
        public const string In_capHolderHome = "0";
        public const string In_pwrDrawer = "1";
        public const string In_NeedleDetected = "2";
        public const string In_3 = "3";
        public const string In_slidingDoor = "4";
        public const string In_drawerOverflow = "5";
        public const string In_drawerClose = "6";
        public const string In_7 = "7";
    }
    public static class TrinamicMuxInputs
    // multiplexed by Out_Multiplexer
    {
        public const string InX_salineBag = "0";
        public const string InX_Vial1 = "1";
        public const string InX_vial2 = "2";
        public const string InX_vial3 = "3";
        public const string InX_vial4 = "4";
        public const string InX_vial5 = "5";
        public const string InX_vial6 = "6";
        public const string InX_mux7 = "7";
    }
    public static class SwitchOutputs
    {
        public const string GreenLED = "0";
        public const string RedLED = "1";
        public const string Out_PulseCapHolder = "2";
        public const string Out_VibrateDIR = "3";
        public const string Out_CAPHolderDIR_Down = "4";
        public const string Out_enaVibrate_4 = "5";
        public const string Out_enaVibrate_56 = "6";
        public const string Out_Multiplexer = "7";
    }
    public static class StepsPerMM
    {
        public const double M_VerticalStepsPerMM = 656;
        public const double M_LinearStepsPerMM = 126;
        public const double M_ArmStepsPerMM = 40;
        public const double M_PistonStepsPerMM = 656;
        public const double M_RotateStepsPerMM = 255;
        public const double M_disposeMicroStepsPerMM = 400;
        public const double M_capHolderMicroStepsPerMM = 50;
    }
    public static class Instruction
    {
        public const string RunApplication = "129";
        public const string SetAxisParameter = "5";
        public const string GetAxisParameter = "6";
        public const string StoreAxisParameter = "7";
        public const string SetGlobalParameter = "9";
        public const string GetGlobalParameter = "10";
        public const string StoreGlobalParameter = "11";
        public const string SetSwitchOutput = "14";
        public const string GetGlobalInOut = "15";
        public const string ResetRobotSoftware = "131";
    }
    public static class AddressBank
    {
        public const string Trinamic = "1";             // trinamic module address
        public const string GetSystemBank = "0";
        public const string GetParameterBank = "2";
        public const string CmdSpecificAddress = "1";
        public const string setOutputs = "2";
        public const string getDigitalInputs = "0";
        public const string actualPosition = "1";
    }
    public static class Values
    {
        public const string Forward = "1";              // move forward
        public const string Backward = "-1";            // move backward
    }
    public static class OutputStates                    // OnOff
    {
        public const string ON = "1";    
        public const string OFF = "0";   
    }
    public static class SystemVariables                 // GP
    {
        public const string GB_currentTrinamicSWversion = "0";
        public const string GB_Syringe_Type = "1";
        public const string GB_2 = "2";   
        public const string GB_InitDone = "3";
        public const string GB_4 = "4"; 
        public const string GB_CurrentState = "5";
        /*
            WAITING_INIT_CM         = 10       // this the state after the power up as well
            RUNNING_INIT_CM         = 20
            WAITING_DISPENSE     = 30       // at this state it is possible as well to run INIT_CM_DOSE (6)
            RUNNING_DISPENSE     = 40
            STATE_50             = 50
            STATE_60             = 60
            STOPPED_ON_ERROR     = 70
            STOPPED_TIME_OUT     = 80
            ABORTED              = 90
        */
        public const string GB_HardwareSerialNumber = "6";
        public const string GB_7 = "7";  
        public const string GB_verticalHomePos = "8";
        public const string GB_GB_BumpPosVert = "9"; 
        public const string GB_needleLength = "10"; 
        public const string GB_needleGauge = "11"; 
        public const string GB_cyclesTotal = "12";
        public const string GB_13 = "13";   
        public const string GB_readyToDraw = "14"; 
        public const string GB_initialVolume = "15"; 
        public const string GB_motorIsMoving = "16";
        public const string GB_17 = "17"; 
        public const string GB_HomingDone = "18";
        public const string GB_UnitsToMoveManual = "19";    
        public const string GB_20 = "20";    
        public const string GB_airToPullBefore = "21";    
        public const string GB_22 = "22";    
        public const string GB_23 = "23";    
        public const string GB_24 = "24";    
        public const string GB_motorNumForHome = "25";   
        public const string GB_26 = "26";   
        public const string GB_microLtoWithdraw = "27";   

        public const string GB_errors_syringe_bag = "28";    
        /*
 // bit errors for parameter 28:
    BitEr_bagIsMissing        = %00000000001 // bit    1  
    BitEr_syringePoppedOut    = %00000000010 // bit    2  syringe popped out during cycle
    BitEr_g3                  = %00000000100 // bit    4  
    BitEr_g4                  = %00000001000 // bit    8
    BitEr_g5                  = %00000010000 // bit   16
    BitEr_machineAborted      = %00000100000 // bit   32
//  BitEr_needleBentRotate    = %00001000000 // bit   64
    BitEr_g8                  = %00010000000 // bit  128
    BitEr_g9                  = %00100000000 // bit  256
        */
        public const string GB_any_Error = "29";     //1 = will not check for pig removal (GP 28 error)

        public const string GB_errors_M_verticalMotor = "30";
        public const string GB_errors_M_linearMotor = "31";
        public const string GB_errors_M_armMotor = "32";
        public const string GB_errors_M_pistonMotor = "33";
        public const string GB_errors_M_headRotateMotor = "34";
        public const string GB_errors_M_disposeMotor = "35";
        public const string GB_errors_M_capHolderMotor = "36";

        /*      motor errors for each of parameters 30-36:
    BitEr_leftRefSensor  =  %00000001 // bit   1  left ref sensor
    BitEr_rightRefSensor =  %00000010 // bit   2  right ref sensor
    BitEr_homeNotFound   =  %00000100 // bit   4  did not find home
    BitEr_TimeOut        =  %00001000 // bit   8
//  BitEr_calibrationErr =  %00010000 // bit  16
    BitEr_m6             =  %00100000 // bit  32
    BitEr_m7             =  %01000000 // bit  64
    BitEr_m8             =  %10000000 // bit 128
        */
        public const string GB_errors_Vial_1 = "37";
        public const string GB_errors_Vial_2 = "38";
        public const string GB_errors_Vial_3 = "39";
        public const string GB_errors_Vial_4 = "40";
        public const string GB_errors_Vial_5 = "41";
        public const string GB_errors_Vial_6 = "42";
        // bit errors for parameter 37-42 Vials:
        /*
    BitEr_v1                = %00000000001 // bit    1  
    BitEr_VialMissing       = %00000000010 // bit    2  
    BitEr_VialPoppedOut     = %00000000100 // bit    4  vial popped out during cycle
    BitEr_v4                = %00000001000 // bit    8
    BitEr_v5                = %00000010000 // bit   16
    BitEr_v6                = %00000100000 // bit   32
    BitEr_v7                = %00001000000 // bit   64
    BitEr_v8                = %00010000000 // bit  128
        */
        public const string GB_errors_findHome = "43";
        /*
         // bit errors for parameter 43
            BitEr_syringeIsInwhileFindHome      =  %00000001 // bit   1
            BitEr_h2                            =  %00000010 // bit   2
            BitEr_capHolderIsInWhileFindHome    =  %00000100 // bit   4
            BitEr_h4                            =  %00001000 // bit   8
            BitEr_h5                            =  %00010000 // bit  16
            BitEr_h6                            =  %00100000 // bit  32
            BitEr_h7                            =  %01000000 // bit  64
            BitEr_h8                            =  %10000000 // bit 128
                */
        public const string GB_errors_wrong_PC_command = "44";
        /*
// bit errors for parameter 44
BitEr_expecting_GP5_10_OR_30        =  %00000001 // bit   1
BitEr_expecting_WAITING_DISPENSE    =  %00000010 // bit   2
BitEr_c3                            =  %00000100 // bit   4
BitEr_vibrateParemeterError         =  %00001000 // bit   8
BitEr_c5                            =  %00010000 // bit  16
BitEr_c6                            =  %00100000 // bit  32
BitEr_c7                            =  %01000000 // bit  64
BitEr_c8                            =  %10000000 // bit 128
        */
        public const string GB_special_Error = "45";
        /*
          //  motor errors for each of parameter 45
            BitEr_SyringeIsIn         =  %00000001 // bit   1  FIND_HOMES error or INIT_DRAW_DOSE, Syringe is in the system
            BitEr_SyringeMissing      =  %00000010 // bit   2  DRAW_DOSE error, missing the syringe
            BitEr_No_vials            =  %00000100 // bit   4  DRAW_DOSE error, missing the vial
            BitEr_volumeExceedsLimits =  %00001000 // bit   8
            BitEr_s4                  =  %00010000 // bit  16
            BitEr_s5                  =  %00100000 // bit  32
            BitEr_s6                  =  %01000000 // bit  64
            BitEr_s7                  =  %10000000 // bit 128
        */

        public const string GB_RecapPositionlinear = "46";
        public const string GB_ArmHomePosition = "47";
        public const string GB_PistonHomePos = "48";
        public const string GB_HeadRotateHomePos = "49";
        public const string GB_LinearCenterOfBag = "50";
        public const string GB_DisposeHomePos = "51";
        public const string GB_linearHomePos = "52";
        public const string GB_linearBagToVial0 = "53";
        public const string GB_CapHolderHomePos = "54";
        public const string GB_armAtBottom = "55";

        // ***********************************************************************
        // from here on, GB cannot be stored (STGP) in the EEPROM
        // ***********************************************************************
        public const string GB_56 = "56";
        public const string GB_57 = "57";
        public const string GB_58 = "58";

        public const string GB_59 = "59";
        public const string GB_needleLengthInMicroSteps = "60";

        public const string GB_61 = "61";
        public const string GB_62 = "62";
        public const string GB_63 = "63";
        public const string GB_64 = "64";    // was motorIsMoving = "64";
        public const string GB_65 = "65";
        public const string GB_66 = "66";
        public const string GB_67 = "67";
        public const string GB_68 = "68";
        //public const string GB_P100XposOnWaste = "69";

        public const string GB_armMicroStepsPerMM = "70";
        //public const string GB_limitarmBentMicroS = "71";
        //public const string GB_NeedleArmError = "72";
        public const string GB_73 = "73";
        public const string GB_74 = "74";
        public const string GB_75 = "75";
        public const string GB_pistonMicroStepPer100microL = "76";
        public const string GB_77 = "77";
        public const string GB_78 = "78";
        public const string GB_79 = "79";
        public const string GB_80 = "80";
        public const string GB_rotateMicroStepsPerMM = "81";
        public const string GB_82 = "82";
        public const string GB_NeedleVialError = "83";
        public const string GB_84 = "84";
        public const string GB_85 = "85";
        public const string GB_86 = "86";
        public const string GB_CapHolderPulses = "87";
        public const string GB_slowCapHolder = "88";
        public const string GB_capWaitLoops = "89";
        public const string GB_CapLimitSwitchDisable = "90";
        // microLtoWithdraw values
        public const string GB_microLtoWithdraw_1 = "91";
        public const string GB_microLtoWithdraw_2 = "92";
        public const string GB_microLtoWithdraw_3 = "93";
        public const string GB_microLtoWithdraw_4 = "94";
        public const string GB_microLtoWithdraw_5 = "95";
        public const string GB_microLtoWithdraw_6 = "96";
        public const string GB_CmdInProcess = "97";
        public const string GB_vialsExist = "98";
/*
    Bit_vial1      =  %00000001 // bit   1
    Bit_vial2      =  %00000010 // bit   2
    Bit_vial3      =  %00000100 // bit   4
    Bit_vial4      =  %00001000 // bit   8
    Bit_vial5      =  %00010000 // bit  16
    Bit_vial6      =  %00100000 // bit  32
    Bit_bag        =  %01000000 // bit  64
*/
        public const string GB_microLitterInBAG = "99";
        public const string GB_moveManualBackwards = "100";   // 1 = forward;  -1 = backward
        // vialSize_mL values
        public const string GB_vialSize_uL_1 = "101";    
        public const string GB_vialSize_uL_2 = "102";
        public const string GB_vialSize_uL_3 = "103";
        public const string GB_vialSize_uL_4 = "104";    
        public const string GB_vialSize_uL_5 = "105";
        public const string GB_vialSize_uL_6 = "106";
        public const string GB_BagSize_mL = "107";

        public const string GB_CapHolderHolds = "86"; //  "1" = holds,    "0" = open

        public const string GB_rotateVialsDown = "111";      // 1600
        /*
            Bit_vibrate0      =  %00000001 // bit   1
            Bit_vibrate1      =  %00000010 // bit   2
            Bit_vibrate2      =  %00000100 // bit   4
            Bit_vibrate3      =  %00001000 // bit   8
            Bit_vibrate4      =  %00010000 // bit  16
            Bit_vibrate5      =  %00100000 // bit  32
         */
        // vibration time for vials [seconds]
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public const string GB_vibrationTime_4 = "114";              // [sec] input
        public const string GB_vibrationTime_4_calc = "115";
        public const string GB_vibrationTime_56 = "116";             // [sec] input
        public const string GB_vibrationTime_56_calc = "117";
        public const string GB_118 = "118";

// vibration strength for vials 4,56 -  1 / 2 / 3 / 4 / 5
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public const string GB_vibrStrengthPercentCalc = "119";      // set up %
        public const string GB_PwmDutyCycleMS = "120";               // calculated [ms]
        public const string GB_vibrator4done = "121";
        public const string GB_vibrator56done = "122";
        public const string GB_vibrationDutyCyclePercent = "123";    // 10-100 [%]  input

        // vibration cycle time for vials 4,56 - [ms]
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public const string GB_vibrationCycleMS = "124";              // calculation to ms
        public const string GB_vibrationHz = "125";                   // data input  [Hz]  input
        public const string GB_126 = "126";
        public const string GB_127 = "127";
        public const string GB_128 = "128";

        // current syringe
        public const string GB_Max_Volume_current = "160";
        public const string GB_microL_per_100mm_current = "161";
        public const string GB_Syring_Length_current = "162";
        public const string GB_163 = "163";

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // bubbles
        public const string GB_numberOfKicksTemp = "182";
        public const string GB_numberOfKicks = "183";              //  10
        public const string GB_rotateBubblesUM = "184";            // 750 = 0.75mm
        public const string GB_pistonBubblesPullMicroL = "185";    // 300 * microL = 0.3ml
        public const string GB_pistonBubblesPushMicroL = "186";    // 150 * microL = 0.3ml

        public const string GB_dispenseCycleTime01s = "187";       // calculates the time of the cycle evry 0.1 s
        public const string GB_dispenseCycleTimeMS = "188";        // calculates the time of the cycle evry ms
        public const string GB_PigWasReplaced = "190";   
        public const string GB_inHomeCapHolderMotor = "191";       
        public const string GB_MulCenterOfVial = "192";            // for running average calculations
        public const string GB_adjustmentsTotal = "193";
        public const string GB_TouchedLeftRef = "195";
        public const string GB_dipperInterruptHight = "196";

        public const string GB_microLinVial1 =  "197";
        public const string GB_microLinVial2 =  "198";
        public const string GB_microLinVial3 =  "199";
        public const string GB_microLinVial4 =  "200";
        public const string GB_microLinVial5 =  "201";
        public const string GB_microLinVial6 =  "202";

public const string GB_InterruptCount = "255";             // temorary variable 5

                                  
    }
    public static class GeneralFunctions
    {
        public const string FIRST_RUN = "0";                      //Runs on power on
        public const string CLEAR_ALL_ERRORS = "4";
        public const string INIT_CM = "6";
        public const string DRAW_DOSE = "8";
        public const string ABORT = "12";
        public const string FIND_HOMES = "18";
        public const string HomeCalibration = "20";
        public const string LEDS_OFF = "22";
        public const string RED_ON = "24";
        public const string GREEN_ON = "26";
        public const string YELLOW_ON = "28";
        public const string positionVerticalMotor = "30";
        public const string initInterrups = "32";
        public const string verifyVIAL = "34";
        public const string screenAllVials = "36";
        public const string PositionHeadRotateMotor = "38";
        public const string verticalMotorTOerr = "40";
        public const string homeLinearMotor = "42";
        public const string capHolderMotorTOerr = "44";
        public const string pistonMotorTOerr = "46";
        public const string headRotateMotorTOerr = "48";
        public const string homeArmMotor = "50";
        public const string HomeDisposeMotor = "52";
        public const string homeHeadRotateMotor = "58";
        public const string checkSyringeSensor = "60";
        public const string checkNoSyringe = "62";
        public const string checkSyrPoppedOut = "66";
        public const string startPullAir70 = "72";
        public const string decapSyringe = "74";
        public const string moveBelowVial = "76";
        public const string insertNeedle = "78";
        public const string push70air = "80";
        public const string drawVial = "82";
        public const string ClearRunningErrors = "84";
        public const string moveSlowlyBottom = "86";
        public const string bumpPlunger = "88";
        public const string recapSyringe = "90";
        public const string startHomeDisposeMotor = "92";
        public const string drawVialMoreBack = "100";
        public const string homeVerticalMotor = "102";
        public const string homePistonMotor = "104";
        public const string incrementCycles = "112";
        public const string Vibrate = "114";
        public const string checkVialPoppedOut = "116";
        public const string stopVibrate = "120";
        public const string homeCapHolderMotor = "122";
        public const string startHomePistonMotor = "124";
        public const string defaultVibrate = "126";
        public const string VerticalManual = "136";
        public const string LinearMotorManual = "138";
        public const string armMotorManual = "140";
        public const string PistonManual = "142";
        public const string RotationManual = "144";
        public const string DisposeManual = "154";
        public const string CapHolderManual = "156";
        public const string testCapHolder = "158";
    }
}
