using System;

namespace Vertx.Utilities
{
	/// <summary>
	/// Proportional data associated with proportional sliders can be calculated using an instance of this class.
	/// </summary>
	public class ProportionalValues
	{
		private readonly float[] _values;
		public float Total { get; }
		public event Action<int, float> OnValueChanged;

		public ProportionalValues(float[] values, float total = 1)
		{
			_values = values;
			Total = total;
			Setup();
		}

		public ProportionalValues(int count, float total = 1)
		{
			_values = new float[count];
			Total = total;
			Setup();
		}

		private void Setup()
		{
			float valueTotal = 0;
			foreach (float value in _values)
				valueTotal += value;

			if (valueTotal < 0.00001f)
			{
				float toBeDistributedDivided = Total / (_values.Length - 1);
				for (var i = 0; i < _values.Length; i++)
					_values[i] = toBeDistributedDivided;
			}
			else
			{
				for (int i = 0; i < _values.Length; i++)
					_values[i] /= valueTotal * Total;
			}
		}

		public int Length => _values.Length;

		public float this[int i]
		{
			get => GetValue(i);
			set => SetValue(i, value);
		}

		private float GetValue(int index)
		{
			if (index < 0 || index >= _values.Length)
				throw new IndexOutOfRangeException($"Index {index} does not exist in the {nameof(ProportionalValues)} current state. " +
				                                   "You should create a new instance if the array size has changed.");
			return _values[index];
		}

		private void SetValue(int index, float value)
		{
			if (index < 0 || index >= _values.Length)
				throw new IndexOutOfRangeException();
			_values[index] = value;
			float toBeDistributed = Total - value;

			//If there's nothing to be distributed at all
			if (toBeDistributed < 0.00001f)
			{
				//Reset all the other sliders to 0
				for (int i = 0; i < _values.Length; i++)
				{
					if (i == index) continue;
					_values[i] = 0;
					OnValueChanged?.Invoke(i, 0);
				}

				return;
			}

			//Calculate the total amount of all the other sliders
			float otherValuesTotal = 0;
			for (int i = 0; i < _values.Length; i++)
			{
				if (i == index) continue;
				otherValuesTotal += _values[i];
			}

			//If none of the values can take up the remaining slack
			if (otherValuesTotal < 0.00001f)
			{
				//just divide it between them all
				float toBeDistributedDivided = toBeDistributed / (_values.Length - 1);
				for (int i = 0; i < _values.Length; i++)
				{
					if (i == index) continue;
					_values[i] = toBeDistributedDivided;
					OnValueChanged?.Invoke(i, toBeDistributedDivided);
				}

				return;
			}

			//Distribute the remaining amount proportionally across the other values
			for (int i = 0; i < _values.Length; i++)
			{
				if (i == index) continue;
				float v = _values[i] / otherValuesTotal * toBeDistributed;
				_values[i] = v;
				OnValueChanged?.Invoke(i, v);
			}
		}
	}
}