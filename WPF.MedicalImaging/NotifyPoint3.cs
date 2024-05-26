using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging
{
    public class NotifyPoint3<T>
    {
        private T x;
        public T X
        {
            get => this.x;
            protected set
            {
                if (!this.x.Equals(value))
                {
                    this.x = value;
                }
            }
        }

        private T y;
        public T Y
        {
            get => this.y;
            protected set
            {
                if (!this.y.Equals(value))
                {
                    this.y = value;
                }
            }
        }

        private T z;
        public T Z
        {
            get => this.z;
            protected set
            {
                if (!this.z.Equals(value))
                {
                    this.z = value;
                }
            }
        }

        public void SetX(T x)
        {
            List<string> changedProperities = new List<string>();
            if (!this.X.Equals(x))
            {
                this.X = x;
                changedProperities.Add("X");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public void SetY(T y)
        {
            List<string> changedProperities = new List<string>();
            if (!this.Y.Equals(y))
            {
                this.Y = y;
                changedProperities.Add("Y");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public void SetZ(T z)
        {
            List<string> changedProperities = new List<string>();
            if (!this.Z.Equals(z))
            {
                this.Z = z;
                changedProperities.Add("Z");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public void SetXY(T x, T y)
        {
            List<string> changedProperities = new List<string>();
            if (!this.X.Equals(x))
            {
                this.X = x;
                changedProperities.Add("X");
            }
            if (!this.Y.Equals(y))
            {
                this.Y = y;
                changedProperities.Add("Y");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public void SetXZ(T x, T z)
        {
            List<string> changedProperities = new List<string>();
            if (!this.X.Equals(x))
            {
                this.X = x;
                changedProperities.Add("X");
            }
            if (!this.Z.Equals(z))
            {
                this.Z = z;
                changedProperities.Add("Z");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public void SetYZ(T y, T z)
        {
            List<string> changedProperities = new List<string>();
            if (!this.Y.Equals(y))
            {
                this.Y = y;
                changedProperities.Add("Y");
            }
            if (!this.Z.Equals(z))
            {
                this.Z = z;
                changedProperities.Add("Z");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }


        public void Set(T x, T y, T z)
        {
            List<string> changedProperities = new List<string>();
            if (!this.X.Equals(x))
            {
                this.X = x;
                changedProperities.Add("X");
            }
            if (!this.Y.Equals(y))
            {
                this.Y = y;
                changedProperities.Add("Y");
            }
            if (!this.Z.Equals(z))
            {
                this.Z = z;
                changedProperities.Add("Z");
            }
            if (changedProperities.Count > 0)
            {
                this.OnChanged(new NotifyPoint3DChangedEventArgs(changedProperities.ToArray()));
            }
        }

        public event EventHandler<NotifyPoint3DChangedEventArgs> Changed;

        public void OnChanged(NotifyPoint3DChangedEventArgs args)
        {
            if (this.Changed != null)
            {
                this.Changed.Invoke(this, args);
            }
        }
    }


    public class NotifyPoint3DChangedEventArgs : EventArgs
    {

        public NotifyPoint3DChangedEventArgs(string[] propertyNames)
        {
            this.PropertyNames = propertyNames;
        }

        protected virtual string[] PropertyNames { get; }

        public bool XChanged => this.PropertyNames.Contains("X");

        public bool YChanged => this.PropertyNames.Contains("Y");

        public bool ZChanged => this.PropertyNames.Contains("Z");
    }
}
