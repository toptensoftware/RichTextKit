using System;
using System.Collections.Generic;
using System.Linq;

namespace Topten.RichTextKit.Utils
{
    /// <summary>
    /// Implements an manager for undo operations
    /// </summary>
    /// <typeparam name="T">A context object type (eg: document type)</typeparam>
    public class UndoManager<T>
    {
        /// <summary>
        /// Constructs a new undo manager
        /// </summary>
        /// <param name="context">The document context object</param>
        public UndoManager(T context)
        {
            _maxUnits = 100;
            _context = context;
        }

        /// <summary>
        /// Execute an undo unit and add it to the manager 
        /// </summary>
        /// <param name="unit">The undo unit to execute</param>
        public void Do(UndoUnit<T> unit)
        {
            // Only if not blocked
            if (IsBlocked)
                throw new InvalidOperationException("Attempt to execute undo operation while blocked");

            // Fire start
            if (CurrentGroup == null)
                OnStartOperation();

            // Remember if was modified
            bool wasModified = IsModified;

            try
            {
                // Do it
                unit.Do(_context);
                Add(unit);
            }
            finally
            {
                // End operation if not in a group
                if (CurrentGroup == null)
                    OnEndOperation();

                // Fire modified changed
                if (wasModified != IsModified)
                    OnModifiedChanged();
            }
        }

        /// <summary>
        /// Undoes the last performed operation
        /// </summary>
        public void Undo()
        {
            // Check if can
            if (!CanUndo)
                return;

            // Remember if was modified
            bool wasModified = IsModified;

            // Fire start op
            OnStartOperation();

            // Seal the currently open item
            Seal();

            // Undo
            Block();
            _units[_currentPos - 1].Undo(_context);
            Unblock();

            // Update position
            _currentPos--;

            // End operation
            OnEndOperation();

            // Fire modified event
            if (wasModified != IsModified)
                OnModifiedChanged();
        }

        /// <summary>
        /// Redoes previously undone operations
        /// </summary>
        public void Redo()
        {
            // Check if can
            if (!CanRedo)
                return;

            // Remember if modified
            bool wasModified = IsModified;

            // Fire start events
            OnStartOperation();

            // Seal the last item
            Seal();

            // Undo
            Block();
            _units[_currentPos].Redo(_context);
            Unblock();

            // Update position
            _currentPos++;

            // Fire end events
            OnEndOperation();

            // Fire modified event
            if (wasModified != IsModified)
                OnModifiedChanged();
        }

        /// <summary>
        /// Stars a group operation
        /// </summary>
        /// <param name="description">A user readable description of the operation</param>
        /// <returns>An IDisposable that when disposed will close the group</returns>
        public IDisposable OpenGroup(string description)
        {
            return OpenGroup(new UndoGroup<T>(description));
        }

        /// <summary>
        /// Stars a group operation
        /// </summary>
        /// <param name="group">The UndoGroup to be used</param>
        /// <returns>An IDisposable that when disposed will close the group</returns>
        public IDisposable OpenGroup(UndoGroup<T> group)
        {
            if (IsBlocked)
                throw new InvalidOperationException("Attempt to add undo group while blocked");

            // First group?
            if (_openGroups.Count == 0)
                OnStartOperation();

            // Notified it's open
            group.OnOpen(_context);

            // Add to stack
            _openGroups.Push(group);

            // Seal the last item
            Seal();

            // Return a disposable
            if (_groupDisposer == null)
                _groupDisposer = new GroupDisposer(this);
            return _groupDisposer;
        }

        /// <summary>
        /// Ends the current group operation
        /// </summary>
        public void CloseGroup()
        {
            if (IsBlocked)
                throw new InvalidOperationException("Attempt to end undo group while blocked");

            if (CurrentGroup == null)
                throw new InvalidOperationException("Attempt to end unopened undo group");

            // Remember the group
            var group = CurrentGroup;

            // Pop the group and add it to either the outer open
            // group, or the main undo stack
            Add(_openGroups.Pop());

            // Notify closed
            group.OnClose(_context);

            // End operation if no open groups
            if (_openGroups.Count == 0)
                OnEndOperation();
        }

        /// <summary>
        /// Clear and reset the undo manager
        /// </summary>
        public void Clear()
        {
            _units.Clear();
            _currentPos = 0;
            _unmodifiedPos = -1;
            _openGroups.Clear();
            _blockDepth = 0;
        }

        /// <summary>
        /// Check if can undo
        /// </summary>
        public bool CanUndo
        {
            get
            {
                return GetUndoUnit() != null;
            }
        }

        /// <summary>
        /// Check if can redo
        /// </summary>
        public bool CanRedo
        {
            get
            {
                return GetRedoUnit() != null;
            }
        }

        /// <summary>
        /// Gets the description of the next undo operation  
        /// </summary>
        public string UndoDescription
        {
            get
            {
                var unit = GetUndoUnit();
                if (unit == null)
                    return null;

                return unit.Description;
            }
        }

        /// <summary>
        /// Gets the description of the next redo operation  
        /// </summary>
        public string RedoDescription
        {
            get
            {
                var unit = GetRedoUnit();
                if (unit == null)
                    return null;

                return unit.Description;
            }
        }

        /// <summary>
        /// Event fired when any operation (or group of operations) starts
        /// </summary>
        public event Action StartOperation;

        /// <summary>
        /// Event fired when any operation (or group of operations) ends
        /// </summary>
        public event Action EndOperation;

        /// <summary>
        /// Fired when the modified state of the document changes
        /// </summary>
        public event Action ModifiedChanged;

        /// <summary>
        /// Checks if the document is currently modified
        /// </summary>
        public bool IsModified
        {
            get
            {
                return _unmodifiedPos != _currentPos;
            }
        }

        /// <summary>
        /// Mark the document as currently unmodified
        /// </summary>
        /// <remarks>
        /// Typically this method would be called when the document
        /// is saved.
        /// </remarks>
        public void MarkUnmodified()
        {
            // Remember if was modified
            bool wasModified = IsModified;

            // Mark as currently unmodified
            _unmodifiedPos = _currentPos;

            // Prevent additions to the open item
            Seal();

            // Fire modified changed event
            if (wasModified)
                OnModifiedChanged();
        }

        /// <summary>
        /// Seals the last item to prevent changes
        /// </summary>
        public void Seal()
        {
            if (_units.Count > 0)
                _units[_units.Count - 1].Seal();
        }

        /// <summary>
        /// Get the current unsealed unit
        /// </summary>
        /// <returns>The unsealed unit if available, otherwise null</returns>
        public UndoUnit<T> GetUnsealedUnit()
        {
            // Don't allow coalescing while we have open groups.
            if (_openGroups.Count > 0)
                return null;

            var unit = GetUndoUnit();

            if (unit == null)
                return null;

            if (unit.Sealed)
                return null;

            return unit;
        }

        /// <summary>
        /// Retrieves the unit that would be executed on Undo
        /// </summary>
        /// <returns>An UndoUnit, or null</returns>
        public UndoUnit<T> GetUndoUnit()
        {
            if (_currentPos > 0)
                return _units[_currentPos - 1];
            else
                return null;
        }

        /// <summary>
        /// Retrieves the unit that would be executed on Redo
        /// </summary>
        /// <returns>An UndoUnit, or null</returns>
        public UndoUnit<T> GetRedoUnit()
        {
            if (_currentPos < _units.Count)
                return _units[_currentPos];
            else
                return null;
        }

        /// <summary>
        /// Notifies that an operation (or group of operations) is about to start
        /// </summary>
        protected virtual void OnStartOperation()
        {
            StartOperation?.Invoke();
        }

        /// <summary>
        /// Notifies that an operation (or group of operations) has finished
        /// </summary>
        protected virtual void OnEndOperation()
        {
            EndOperation?.Invoke();
        }

        /// <summary>
        /// Notifies when the modified state of the document changes
        /// </summary>
        protected virtual void OnModifiedChanged()
        {
            ModifiedChanged?.Invoke();
        }

        /// <summary>
        /// Adds a unit to the undo manager without executing it
        /// </summary>
        /// <param name="unit">The UndoUnit to add</param>
        void Add(UndoUnit<T> unit)
        {
            if (IsBlocked)
                throw new InvalidOperationException("Attempt to add undo operation while blocked");

            if (CurrentGroup != null)
            {
                CurrentGroup.Add(unit);
            }
            else
            {
                RemoveAllRedoUnits();
                _units.Add(unit);

                // Limit undo stack size
                if (_units.Count > _maxUnits)
                {
                    // Update unmodified index
                    if (_unmodifiedPos >= 0)
                        _unmodifiedPos--;

                    // Remove
                    _units.RemoveAt(0);
                }
                else
                {
                    _currentPos++;
                }
            }
        }

        /// <summary>
        /// Removes all units in the redo queue
        /// </summary>
        void RemoveAllRedoUnits()
        {
            System.Diagnostics.Debug.Assert(_openGroups.Count == 0);

            // If the unmodified position has been undone
            // we can never get back to clean position
            if (_unmodifiedPos > _currentPos)
            {
                _unmodifiedPos = -1;
            }

            // Remove redo units
            while (_currentPos < _units.Count)
            {
                _units.RemoveAt(_currentPos);
            }

            // Seal the last item
            Seal();
        }

        /// <summary>
        /// Checks if the undo manager is currently blocked
        /// </summary>
        bool IsBlocked
        {
            get
            {
                return _blockDepth > 0;
            }
        }

        /// <summary>
        /// Blocks the undo manager
        /// </summary>
        void Block()
        {
            _blockDepth++;
            Seal();
        }

        /// <summary>
        /// Unblocks the undo manager
        /// </summary>
        void Unblock()
        {
            if (_blockDepth == 0)
                throw new InvalidOperationException("Attempt to unblock already unblocked undo manager");
            _blockDepth--;
        }

        /// <summary>
        /// Get the currently undo group
        /// </summary>
        UndoGroup<T> CurrentGroup
        {
            get
            {
                if (_openGroups.Count > 0)
                    return _openGroups.Peek();
                else
                    return null;
            }
        }

        class GroupDisposer : IDisposable
        {
            public GroupDisposer(UndoManager<T> owner)
            {
                _owner = owner;
            }

            UndoManager<T> _owner;

            public void Dispose()
            {
                _owner.CloseGroup();
            }
        }

        // Private members
        T _context;
        List<UndoUnit<T>> _units = new List<UndoUnit<T>>();
        Stack<UndoGroup<T>> _openGroups = new Stack<UndoGroup<T>>();
        int _currentPos;
        int _unmodifiedPos;
        int _maxUnits;
        int _blockDepth;
        GroupDisposer _groupDisposer;
    }


    /// <summary>
    /// Base class for all undo units
    /// </summary>
    /// <typeparam name="T">The document context type</typeparam>
    public abstract class UndoUnit<T>
    {
        /// <summary>
        /// Constructs a new UndoUnit
        /// </summary>
        public UndoUnit()
        {
        }

        /// <summary>
        /// Constructs a new UndoUnit with a description
        /// </summary>
        /// <param name="description">The description of this unit</param>
        public UndoUnit(string description)
        {
            _description = description;
        }

        /// <summary>
        /// Gets the description of this undo unit
        /// </summary>
        public virtual string Description
        {
            get { return _description; }
            protected set { _description = value; }
        }

        /// <summary>
        /// Instructs the unit to execute the "Do" operation
        /// </summary>
        /// <param name="context">The document context object</param>
        public abstract void Do(T context);

        /// <summary>
        /// Instructs the unit to execute the "ReDo" operation
        /// </summary>
        /// <remarks>
        /// The default implementation simply calls "Do"
        /// </remarks>
        /// <param name="context">The document context object</param>
        public virtual void Redo(T context)
        {
            Do(context);
        }

        /// <summary>
        /// Instructs the unit to execute the "Undo" operation
        /// </summary>
        /// <param name="context">The document context object</param>
        public abstract void Undo(T context);

        /// <summary>
        /// Informs the unit that no subsequent coalescing operations
        /// will be appended to this unit
        /// </summary>
        public virtual void Seal() 
        {
            _sealed = true;
        }

        /// <summary>
        /// Checks is this item is sealed
        /// </summary>
        public bool Sealed => _sealed;

        /// <summary>
        /// Gets or sets the group that owns this undo unit
        /// </summary>
        /// <remarks>
        /// Will be null if the undo unit isn't within a group operation
        /// </remarks>
        public UndoGroup<T> Group { get; set; }

        // Private members
        string _description;
        bool _sealed;
    }


    /// <summary>
    /// Implements an Undo unit that groups other units
    /// into a single operation
    /// </summary>
    /// <typeparam name="T">The document context type</typeparam>
    public class UndoGroup<T> : UndoUnit<T>
    {
        /// <summary>
        /// Constructs a new UndoGroup with a description
        /// </summary>
        /// <param name="description">The description</param>
        public UndoGroup(string description) : base(description)
        {
        }

        /// <summary>
        /// Notifies this group that it's been opened
        /// </summary>
        /// <param name="context">The document context object</param>
        public virtual void OnOpen(T context)
        {
        }

        /// <summary>
        /// Notifies this group that it's been closed
        /// </summary>
        /// <param name="context">The document context object</param>
        public virtual void OnClose(T context)
        {
        }

        /// <summary>
        /// Adds a unit to this group
        /// </summary>
        /// <param name="unit">The UndoUnit to be added</param>
        public void Add(UndoUnit<T> unit)
        {
            unit.Group = this;
            _units.Add(unit);
        }

        /// <summary>
        /// Inserts an unit to this group
        /// </summary>
        /// <param name="position">The position at which the unit should be inserted</param>
        /// <param name="unit">The UndoUnit to be inserted</param>
        public void Insert(int position, UndoUnit<T> unit)
        {
            unit.Group = this;
            _units.Insert(position, unit);
        }

        /// <summary>
        /// Gets the last UndoUnit in this group
        /// </summary>
        public UndoUnit<T> LastUnit
        {
            get
            {
                if (_units.Count > 0)
                    return _units[_units.Count - 1];
                else
                    return null;
            }
        }

        /// <summary>
        /// Get the list of units in this group
        /// </summary>
        public IReadOnlyList<UndoUnit<T>> Units => _units;

        /// <summary>
        /// The method on the UndoGroup class is never called by the 
        /// UndoManager Never. See OnOpen and OnClose instead which
        /// are called as the group is constructed
        /// </summary>
        public override void Do(T context)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Redo(T context)
        {
            foreach (var u in _units)
            {
                u.Redo(context);
            }
        }

        /// <inheritdoc />
        public override void Undo(T context)
        {
            foreach (var u in _units.Reverse<UndoUnit<T>>())
            {
                u.Undo(context);
            }
        }

        // Private members
        List<UndoUnit<T>> _units = new List<UndoUnit<T>>();
    }

}
