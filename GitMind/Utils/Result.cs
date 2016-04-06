using System;
using System.Data;


namespace GitMind.Utils
{
	public class Result
	{
		public Result(Error error)
		{
			Error = error;
		}


		public Error Error { get; }

		public static Result<T> From<T>(T result) => new Result<T>(result);
	}


	public class Result<T> : Result
	{
		private readonly T value;

		public Result(T value)
			: base(Error.None)
		{
			this.value = value;
		}

		public Result(Error error)
			: base(error)
		{
		}


		public static implicit operator Result<T>(Error error) => new Result<T>(error);

		public static implicit operator Result<T>(T value) => new Result<T>(value);


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
		public bool IsFaulted => Error != Error.None;


		public Result<T> OnError(Action<Error> errorAction)
		{
			if (IsFaulted)
			{
				errorAction(Error);
			}

			return this;
		}

		public Result<T> OnValue(Action<T> valueAction)
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
	}
}