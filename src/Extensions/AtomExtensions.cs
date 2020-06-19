using System;

namespace LFE.FacialMotionCapture.Extensions {
    public static class AtomExtensions
    {
        /// <summary>
        /// Extension method to get the morph control ui on an atom
        ///
        /// Throws InvalidOperationException if the atom doesn't make sense
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static GenerateDAZMorphsControlUI GetMorphsControlUI(this Atom atom)
        {
            JSONStorable geometry = atom.GetStorableByID("geometry");
            if (geometry == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            DAZCharacterSelector dcs = geometry as DAZCharacterSelector;
            if (dcs == null) throw new InvalidOperationException($"Cannot get morphs control for this atom: {atom.uid}");

            return dcs.morphsControlUI;
        }
    }
}
