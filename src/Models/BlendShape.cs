namespace LFE.FacialMotionCapture.Models
{
    public class BlendShape
    {
        public static readonly int MIN_ID = 0;
        public static readonly int MAX_ID = 51;

        public int Id { get; }
        public string Name {
            get
            {
                return CBlendShape.IdToName(Id);
            }
        }

        public string Group {
            get {
                return CBlendShape.IdToGroup(Id);
            }
        }

        public BlendShape(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BlendShape);
        }

        public bool Equals(BlendShape obj)
        {
            return obj != null && obj.Id == this.Id;
        }

        public bool Equals(int obj)
        {
            return obj == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

}
