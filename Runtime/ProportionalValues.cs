using System;

namespace Vertx.Utilities
{
	/// <summary>
	/// Proportional data associated with proportional sliders can be calculated using an instance of this class.
	/// </summary>
	public class ProportionalValues
	{
		private readonly float[] values;
		private readonly float total;

		public ProportionalValues(float[] values, float total = 1)
		{
			this.values = values;
			this.total = total;

			float valueTotal = 0;
			foreach (float value in values)
				valueTotal += value;

			if (valueTotal < 0.00001f)
			{
				float toBeDistributedDivided = total / (values.Length - 1);
				for (var i = 0; i < values.Length; i++)
					values[i] = toBeDistributedDivided;
			}
			else
			{
				for (int i = 0; i < values.Length; i++)
					values[i] = values[i] / valueTotal * total;
			}
		}

		public float GetValue(int index)
		{
			if (index < 0 || index >= values.Length)
				throw new IndexOutOfRangeException($"Index {index} does not exist in the {nameof(ProportionalValues)} current state. " +
				                                   "You should create a new instance if the array size has changed.");
			return values[index];
		}

		public void SetValue(int index, float value)
		{
			if (index < 0 || index >= values.Length)
				throw new IndexOutOfRangeException();
			values[index] = value;
			float toBeDistributed = total - value;

			//If there's nothing to be distributed at all
			if (toBeDistributed < 0.00001f)
			{
				//Reset all the other sliders to 0
				for (int i = 0; i < values.Length; i++)
				{
					if (i == index) continue;
					values[i] = 0;
				}
				return;
			}

			//Calculate the total amount of all the other sliders
			float otherValuesTotal = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if (i == index) continue;
				otherValuesTotal += values[i];
			}

			//If none of the values can take up the remaining slack
			if (otherValuesTotal < 0.00001f)
			{
				//just divide it between them all
				float toBeDistributedDivided = toBeDistributed / (values.Length - 1);
				for (int i = 0; i < values.Length; i++)
				{
					if (i == index) continue;
					values[i] = toBeDistributedDivided;
				}

				return;
			}
			
			//Distribute the remaining amount proportionally across the other values
			for (int i = 0; i < values.Length; i++)
			{
				if (i == index) continue;
				values[i] = values[i] / otherValuesTotal * toBeDistributed;
			}
		}
	}
}