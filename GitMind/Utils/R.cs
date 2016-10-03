using System;


namespace GitMind.Utils
{
	public class R
	{
		public static R Ok = new R(Error.None);
		public static R NoValue = new R(Error.NoValue);
	

		public R(Error error)
		{
			Error = error;
		}

		public Error Error { get; }

		public bool IsFaulted => Error != Error.None;
		public bool IsOk => Error == Error.None;

		public static R<T> From<T>(T result) => new R<T>(result);

		public static implicit operator R(Error error) => new R(error);
		public static implicit operator R(Exception e) => new R(Error.From(e));

		public override string ToString()
		{
			if (IsFaulted)
			{
				return $"Error: {Error}";
			}

			return "OK";
		}
	}


	public class R<T> : R
	{
		private readonly T value;

		public new static R<T> NoValue = new R<T>(Error.NoValue);

		public R(T value)
			: base(Error.None)
		{
			this.value = value;
		}

		public R(Error error)
			: base(error)
		{
		}


		public static implicit operator R<T>(Error error) => new R<T>(error);
		public static implicit operator R<T>(Exception e) => new R<T>(Error.From(e));

		public static implicit operator R<T>(T value)
		{
			if (value == null)
			{
				throw Asserter.FailFast("Value cannot be null");
			}

			return new R<T>(value);
		}


		public T Value
		{
			get
			{
				if (!IsFaulted)
				{
					return value;
				}

				throw Asserter.FailFast(Error);
			}
		}

		public bool HasValue => !IsFaulted;


		public R<T> OnError(Action<Error> errorAction)
		{
			if (IsFaulted)
			{
				errorAction(Error);
			}

			return this;
		}

		public R<T> OnValue(Action<T> valueAction)
		{
			if (!IsFaulted)
			{
				valueAction(value);
			}

			return this;
		}


		public T Or(T defaultValue)
		{
			if (IsFaulted)
			{
				return defaultValue;
			}

			return Value;
		}


		public override string ToString()
		{
			if (IsFaulted)
			{
				return $"Error: {Error}";
			}

			return value?.ToString() ?? "";
		}
	}
}