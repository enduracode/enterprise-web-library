using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A request duration that, if exceeded, will cause the framework to report an error to the developers.
/// </summary>
[ PublicAPI ]
public enum SlowRequestThreshold {
	/// <summary>
	/// Five hundred milliseconds.
	/// </summary>
	_0500ms = 500,

	/// <summary>
	/// Six hundred milliseconds.
	/// </summary>
	_0600ms = 600,

	/// <summary>
	/// Seven hundred milliseconds.
	/// </summary>
	_0700ms = 700,

	/// <summary>
	/// Eight hundred milliseconds.
	/// </summary>
	_0800ms = 800,

	/// <summary>
	/// Nine hundred milliseconds.
	/// </summary>
	_0900ms = 900,

	/// <summary>
	/// One second.
	/// </summary>
	_1000ms = 1000,

	/// <summary>
	/// Two seconds.
	/// </summary>
	_2000ms = 2000,

	/// <summary>
	/// Three seconds.
	/// </summary>
	_3000ms = 3000,

	/// <summary>
	/// Four seconds.
	/// </summary>
	_4000ms = 4000,

	/// <summary>
	/// Five seconds.
	/// </summary>
	_5000ms = 5000
}