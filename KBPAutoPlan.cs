using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace KBPAutoPlan
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static void Execute(Application app)
        {
            Console.WriteLine("Enter Patient ID");
            string patientID = Console.ReadLine();
            Patient patient = app.OpenPatientById(patientID);
            if (patient == null) { return; }
            patient.BeginModifications();
            Course course = patient.AddCourse();
            ExternalPlanSetup externalPlanSetup = course.AddExternalPlanSetup(patient.StructureSets.FirstOrDefault(x => x.Id == "CT_1"));
            double[] gantrys = new double[] { 200, 240, 280, 320, 0, 40, 80, 120, 160 };
            Console.WriteLine("Creating plan");
            ExternalBeamMachineParameters externalBeamMachineParameters = new ExternalBeamMachineParameters(
                "HESN5",
                "6X",
                600,
                "STATIC",
                "FFF");
            List<double> mweights = new List<double>();
            for (double i=0; i<=1; i+=1.0/178.0)
            {
                mweights.Add(i);
            }

            externalPlanSetup.AddVMATBeam(
                externalBeamMachineParameters,
                mweights,
                30,
                179,
                181,
                GantryDirection.Clockwise,
                0,
                externalPlanSetup.StructureSet.Structures.FirstOrDefault(x => x.DicomType == "PTV").CenterPoint);
            
            Console.WriteLine("Setting optimization options");
            var doseLevels = new Dictionary<string, DoseValue>();
            doseLevels.Add("PTVprost SV marg", new DoseValue(7200, DoseValue.DoseUnit.cGy));
            var sMatches = new Dictionary<string, string>();
            sMatches.Add("PTVprost SV marg","PTV");
            sMatches.Add("Bladder", "Bladder");
            sMatches.Add("Rectum", "Rectum");
            Console.WriteLine("Setting up RapidPlan");
            externalPlanSetup.CalculateDVHEstimates(    "WUSTL Prostate Model",
                                                        doseLevels,
                                                        sMatches);
            var options = new OptimizationOptionsVMAT(OptimizationIntermediateDoseOption.UseIntermediateDose, "Millenium 120");
            Console.WriteLine("Optimizing");
            externalPlanSetup.OptimizeVMAT(options);
            Console.WriteLine("Calculating dose");
            externalPlanSetup.CalculateDose();
            app.SaveModifications();
            
        }
    }
}
