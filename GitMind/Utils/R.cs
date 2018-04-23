using System;


namespace GitMind.Utils
{
	public class R
	{
		public static R Ok = new R(Error.None);
		public static R NoValue = new R(Error.NoValue);


		protected R(Error error)
		{
			Error = error;
		}

		public Error Error { get; }

		public bool IsFaulted => Error != Error.None;
		public bool IsOk => Error == Error.None;
		public string Message => Error.Message;

		public static R<T> From<T>(T result) => new R<T>(result);

		public static implicit operator R(Error error) => new R(error);
		public static implicit operator R(Exception e) => new R(Error.From(e));
		public static implicit operator bool(R r) => r.IsOk;
		public override string ToString() => IsOk ? "OK" : $"Error: {Error}";
	}


	public class R<T> : R
	{
		private readonly T storedValue;

		public new static R<T> NoValue = new R<T>(Error.NoValue);

		public R(T value) : base(Error.None) => this.storedValue = value;

		public R(Error error)
			: base(error)
		{
		}


		public static implicit operator R<T>(Error error) => new R<T>(error);
		public static implicit operator R<T>(Exception e) => new R<T>(Error.From(e));
		public static implicit operator bool(R<T> r) => r.IsOk;

		public static implicit operator R<T>(T value)
		{
			if (value == null)
			{
				throw Asserter.FailFast("Value cannot be null");
			}

			return new R<T>(value);
		}


		public T Value => !IsFaulted ? storedValue : throw Asserter.FailFast(Error);


		public bool HasValue(out T value)
		{
			if (!IsFaulted)
			{
				value = storedValue;
				return true;
			}
			else
			{
				value = default(T);
				return false;
			}
		}

		public T Or(T defaultValue) => IsFaulted ? defaultValue : Value;


		public override string ToString() =>
			IsFaulted ? $"Error: {Error}" : (storedValue?.ToString() ?? "");
	}
}