using System;

namespace Xv2CoreLib.EMP_NEW
{
    public delegate void ParticleChangedEventHandler(object source, ParticleSystemChangedEventArgs e);

    public class ParticleSystemChangedEventArgs : EventArgs
    {
        public enum ParticleChange
        {
            Default //Default change: complete particle system re-simulation required
        }

        public ParticleChange Edit { get; private set; }

        /// <summary>
        /// Initialise a new instance of <see cref="ParticleSystemChangedEventArgs"/> with <see cref="Edit"/> set to <see cref="ParticleChange.Default"/>.
        /// </summary>
        public ParticleSystemChangedEventArgs()
        {
            Edit = ParticleChange.Default;
        }

        public ParticleSystemChangedEventArgs(ParticleChange edit)
        {
            Edit = edit;
        }

        public bool ResimulateRequired()
        {
            return Edit == ParticleChange.Default;
        }
    }
}
