using Perpetuum.ExportedTypes;
using Perpetuum.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    class RobotDataView {
        // Main Status
        public double Speed { get; set; }
        public double Armor { get; set; }
        public double Accumulator { get; set; }
        public double Cargo { get; set; }
        public double Cpu { get; set; }
        public double Reactor { get; set; }
        public double SlopeCapacity { get; set; }

        // Slots
        public int SlotsHead { get; set; }
        public int SlotsLeg { get; set; }
        public int SlotsChassis { get; set; }
        public int SlotsGuns { get; set; }
        public int SlotsMissiles { get; set; }
        public int SlotsIndustrial { get; set; }
        public int SlotsMisc { get; set; }

        // Defense
        public double SurfaceSize { get; set; }
        public double ResistanceChem { get; set; }
        public double ResistanceKinetic { get; set; }
        public double ResistanceSeismic { get; set; }
        public decimal ResistanceThermal { get; set; }
        public decimal ResistanceDemobilizer { get; set; }

        // Targeting
        public decimal SensorStrength { get; set; }
        public decimal LockingRange { get; set; }
        public decimal LockingTime { get; set; }
        public decimal MaxLockedTargets { get; set; }
        public decimal SignalDetection { get; set; }
        public decimal SignalMasking { get; set; }

        // Misc
        public decimal AccumulatorRecharge { get; set; }
        public decimal AccumulatorStability { get; set; }
        public decimal InterferenceEmission { get; set; }
        public decimal InterferenceRadius { get; set; }
        public decimal InterferenceMin { get; set; }
        public decimal InterferenceMax{ get; set; }

        public RobotDataView(Robot input) {
            RobotHead head = input.GetRobotComponent<RobotHead>();
            RobotChassis chassis = input.GetRobotComponent<RobotChassis>();
            RobotLeg legs = input.GetRobotComponent<RobotLeg>();
            RobotInventory inventory = input.Components.OfType<RobotInventory>().SingleOrDefault();

            var props = input.Properties;

            var test3 = input.GetPropertyModifier(AggregateField.speed_max);

            Speed = (double)input.Properties.SingleOrDefault(x=> x.Field == AggregateField.speed_max).Value;
            Armor = input.ArmorMax;
            Accumulator = input.CoreMax;
            Cargo = (int) inventory.GetCapacityInfo()["capacity"];
            Cpu = input.Cpu;
            Reactor = input.ReactorRadiation;
            SlopeCapacity = input.Slope;

            SlotsHead = head.MaxSlots;
            SlotsChassis = chassis.MaxSlots;
            SlotsLeg = legs.MaxSlots;
            
        }
    }
}
