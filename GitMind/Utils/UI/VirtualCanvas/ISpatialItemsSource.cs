using System;
using System.Collections.Generic;
using System.Windows;


namespace GitMind.Utils.UI.VirtualCanvas
{
	/// <summary>
	/// Provides a two-dimensional index of items that can be quickly queried for all items that 
	/// intersect a given rectangle.
	/// </summary>
	/// <remarks>
	/// When the <see cref="ZoomableCanvas"/> is hosting items for an ItemsControl, 
	/// the ItemsControl.ItemsSource can implement this interface to greatly speed up 
	/// virtualization in the canvas.
	/// If any of those conditions are not true, then the canvas must realize every item at least 
	/// once in order to determine its bounds before it can virtualize it, and then once it is 
	/// virtualized it will have no means of moving spontaneously back into view.
	/// </remarks>
	public interface ISpatialItemsSource
	{
		/// <summary>
		/// Gets the entire extent of the index, which is typically the union of all bounding boxes 
		/// of all items within the set.
		/// </summary>
		/// <remarks>
		/// This value is used when determining the extent of the scroll bars when the canvas is 
		/// hosted in a scroll viewer.
		/// </remarks>
		Rect Extent { get; }

		/// <summary>
		/// Gets the set of items that intersect the given rectangle.
		/// </summary>
		/// <param name="rectangle">The area in which any intersecting items are returned.</param>
		/// <returns>A result set of all items that intersect the given rectangle.
		/// </returns>
		/// <remarks>
		/// The enumerator returned by this method is used lazily and sometimes only partially, 
		/// meaning it should return quickly without computing the entire result set immediately for 
		/// best results.
		/// </remarks>
		IEnumerable<int> Query(Rect rectangle);

		/// <summary>
		/// Occurs when the value of the <see cref="Extent"/> property has changed.
		/// </summary>
		event EventHandler ExtentChanged;

		/// <summary>
		/// Occurs when the results of the last query are no longer valid and should be re-queried.
		/// </summary>
		event EventHandler QueryInvalidated;
	}
}