using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeFull.Graphics.Transform;

namespace CodeFull.Graphics
{
    /// <summary>
    /// A collection of Drawable objects
    /// </summary>
    public class DrawableCollection : ICollection<Drawable>, IList<Drawable>, IEnumerable<Drawable>
    {
        /// <summary>
        /// The underlying collection of the drawables
        /// </summary>
        protected IList<Drawable> collection = new List<Drawable>();

        /// <summary>
        /// The owner of this collection
        /// </summary>
        protected Drawable owner;

        protected void ApplyTransform(Drawable item)
        {
            //Matrix4d itemTransform = item.Transform.Value;
            //Transform3DGroup allTrans = new Transform3DGroup();
            //foreach (var t in this.owner.Transform.Children)
            //    allTrans.Children.Add(t);

            //foreach (var t in item.Transform.Children)
            //    allTrans.Children.Add(t);

            //item.Transform = allTrans;

            // TODO keep the objects transforms and append the parent's

            item.Transform = this.owner.Transform;
        }

        public DrawableCollection(Drawable owner)
        {
            this.owner = owner;
        }

        public void Add(Drawable item)
        {
            this.collection.Add(item);
            item.Parent = owner;
            ApplyTransform(item);
        }

        public void Clear()
        {
            foreach (var item in collection)
                item.Parent = null;

            this.collection.Clear();
        }

        public bool Contains(Drawable item)
        {
            return this.collection.Contains(item);
        }

        public void CopyTo(Drawable[] array, int arrayIndex)
        {
            this.collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.collection.IsReadOnly; }
        }

        public bool Remove(Drawable item)
        {
            item.Parent = null;
            return this.collection.Remove(item);
        }

        public IEnumerator<Drawable> GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        public int IndexOf(Drawable item)
        {
            return this.collection.IndexOf(item);
        }

        public void Insert(int index, Drawable item)
        {
            this.collection.Insert(index, item);
            item.Parent = this.owner;
            ApplyTransform(item);
        }

        public void RemoveAt(int index)
        {
            try
            {
                collection[index].Parent = null;
            }
            catch (Exception) { }

            this.collection.RemoveAt(index);
        }

        public Drawable this[int index]
        {
            get
            {
                return this.collection[index];
            }
            set
            {
                this.collection[index] = value;
                if (value != null)
                {
                    value.Parent = this.owner;
                    ApplyTransform(value);
                }
            }
        }
    }
}
