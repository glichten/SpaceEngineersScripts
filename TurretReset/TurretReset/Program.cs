using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script
            gunnerStation = GridTerminalSystem.GetBlockWithName("Gunner's Station") as IMyCockpit;
            gunnerScreen = gunnerStation.GetSurface(0);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        string repeatArg;

        List<IMyMotorAdvancedStator> smallBayHinges = new List<IMyMotorAdvancedStator>();
        List<IMyMotorAdvancedStator> bigBayHinges = new List<IMyMotorAdvancedStator>();
        IMyCockpit gunnerStation;
        IMyTextSurface gunnerScreen;

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "GunsOut" || repeatArg == "GunsOut")
            {
                repeatArg = "GunsOut"; 
                GunsOut();
            }
            else if (argument == "GunsIn" || repeatArg == "GunsIn")
            {
                repeatArg = "GunsIn";
                GunsIn();
            }
            else
            {
                gunnerScreen.WriteText("No argument detected!\n");
                gunnerScreen.WriteText("Pass 'GunsOut' or 'GunsIn' to perform the respective action.", true);
            }
        }

        void GunsIn()
        {
            if (Runtime.UpdateFrequency == UpdateFrequency.None)
            {
                gunnerScreen.WriteText("Guns In routine started....\n");
                GunsInit();
                ResetInit();
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            if (!turretReset)
            {
                ResetTurret();
            }

            if (turretReset)
            {
                foreach (var hinge in bigBayHinges)
                {
                    hinge.TargetVelocityRPM = -3;
                }
                foreach (var piston in turretPistons)
                {
                    WriteOnce("Retracting Turret...");
                    piston.Velocity = -0.5f;
                }
                if (turretPistons[0].CurrentPosition == 0 )
                {
                    turretArmHinge.TargetVelocityRPM = 2;
                    turretHinge.TargetVelocityRPM = -5;
                }
                if (turretArmHinge.Angle > MathHelper.ToRadians(30))
                {
                    WriteOnce("    Complete\nClosing Bay Doors...\n");
                    foreach (var hinge in smallBayHinges)
                    {
                        hinge.TargetVelocityRPM = -5;
                    }
                    foreach (var hinge in bigBayHinges)
                    {
                        hinge.TargetVelocityRPM = 3;
                    }
                }

                if (bigBayHinges[0].Angle > MathHelper.ToRadians(85) && turretArmHinge.Angle > MathHelper.ToRadians(70))
                {
                    gunnerScreen.WriteText("Guns Stowed", true);
                    repeatArg = "";
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }
            }
        }

        void GunsOut()
        {
            if (Runtime.UpdateFrequency == UpdateFrequency.None)
            {
                gunnerScreen.WriteText("Guns Out routine started...\n");
                GunsInit();
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            turretArmHinge.TargetVelocityRPM = -2;
            turretHinge.TargetVelocityRPM = 5;
            turretHinge.UpperLimitDeg = 0;
            WriteOnce("Opening Bay Doors...");
            foreach (var hinge in smallBayHinges)
            {
                hinge.TargetVelocityRPM = 5;
            }

            foreach (var hinge in bigBayHinges)
            {
                hinge.TargetVelocityRPM = -3;
            }

            if (turretArmHinge.Angle < MathHelper.ToRadians(25))
            {
                WriteOnce("    Complete\n");
                foreach (var piston in turretPistons)
                {
                    piston.Velocity = 0.5f;
                }
                WriteOnce("Deploying Turret...\n");
                foreach (var hinge in bigBayHinges)
                {
                    hinge.TargetVelocityRPM = 3;
                }
                WriteOnce("Closing Bay Doors...\n");
            }

            if (bigBayHinges[0].Angle > MathHelper.ToRadians(85) && turretArmHinge.Angle < MathHelper.ToRadians(1))
            {
                turretHinge.UpperLimitDeg = 50;
                turretController.AIEnabled = true;
                gunnerScreen.WriteText("Locked & Loaded!", true);
                repeatArg = "";
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
        }

        void GunsInit()
        {
            gunnerScreen.WriteText("Initializing...\n", true);
            turretArmHinge = GridTerminalSystem.GetBlockWithName("Turret Arm Hinge") as IMyMotorAdvancedStator;
            turretHinge = GridTerminalSystem.GetBlockWithName("Turret Hinge") as IMyMotorAdvancedStator;
            turretController = GridTerminalSystem.GetBlockWithName("Turret Controller") as IMyTurretControlBlock;

            GridTerminalSystem.GetBlockGroupWithName("Explorer - Small Bay Hinges").GetBlocksOfType(smallBayHinges);
            GridTerminalSystem.GetBlockGroupWithName("Explorer - Turret Pistons").GetBlocksOfType(turretPistons);
            GridTerminalSystem.GetBlockGroupWithName("Explorer - Big Bay Hinges").GetBlocksOfType(bigBayHinges);
        }

        // Turret Vars
        List<IMyPistonBase> turretPistons = new List<IMyPistonBase>();
        IMyMotorAdvancedStator turretArmHinge;
        IMyMotorAdvancedStator turretRotor;
        IMyMotorAdvancedStator turretHinge;
        float hingeAngleLower = -50;
        float hingeAngleUpper = 50;
        IMyTurretControlBlock turretController;
        bool turretReset;

        void ResetTurret()
        {
            WriteOnce("Resetting Turret...");
            float rotorAngle = turretRotor.Angle;
            if (rotorAngle != 0)
            {
                if (rotorAngle > 0)
                {
                    turretRotor.LowerLimitDeg = 0;
                    turretRotor.TargetVelocityRPM = -30;
                    rotorAngle = turretRotor.Angle;
                }
                if (rotorAngle < 0)
                {
                    turretRotor.UpperLimitDeg = 360;
                    turretRotor.TargetVelocityRPM = 30;
                    rotorAngle = turretRotor.Angle;
                }
            }

            float hingeAngle = turretHinge.Angle;
            if (hingeAngle != 0)
            {
                if (hingeAngle > 0)
                {
                    turretHinge.LowerLimitDeg = 0;
                    turretHinge.TargetVelocityRPM = -15;
                    hingeAngle = turretHinge.Angle;
                }
                if (hingeAngle < 0)
                {
                    turretHinge.UpperLimitDeg = 0;
                    turretHinge.TargetVelocityRPM = 15;
                    hingeAngle = turretHinge.Angle;
                }
            }

            if (hingeAngle < MathHelper.ToRadians(1) && rotorAngle < MathHelper.ToRadians(1))
            {
                turretRotor.TargetVelocityRPM = 0;
                turretRotor.LowerLimitDeg = float.MinValue;
                turretRotor.UpperLimitDeg = float.MaxValue;

                turretHinge.TargetVelocityRPM = 0;
                turretHinge.LowerLimitDeg = hingeAngleLower;
                turretHinge.UpperLimitDeg = hingeAngleUpper;

                gunnerScreen.WriteText("    Complete\n", true);
                turretReset = true;
                return;
            }

        }

        void ResetInit()
        {
            turretController.AIEnabled = false;
            turretReset = false;

            turretRotor = GridTerminalSystem.GetBlockWithName("Turret Rotor") as IMyMotorAdvancedStator;
            if (turretRotor == null)
            {
                gunnerScreen.WriteText("Unable to find Turret Rotor");
                return;
            }

            turretHinge = GridTerminalSystem.GetBlockWithName("Turret Hinge") as IMyMotorAdvancedStator;
            if (turretHinge == null)
            {
                gunnerScreen.WriteText("Unable to find Turret Hinge");
                return;
            }
            hingeAngleLower = turretHinge.LowerLimitDeg;
            hingeAngleUpper = turretHinge.UpperLimitDeg;

        }

        void WriteOnce(string input)
        {
            var currentText = gunnerScreen.GetText();
            if (!currentText.Contains(input)) gunnerScreen.WriteText(input, true);
        }
    }
}
