# Deliberate Omissions

## Advanced CSS pre-processing

We don't want to make it easy for people to generate CSS in a way that duplicates existing CSS functionality but fails to understand how CSS is designed to work. We think that some of the ideas in LESS and SASS are not necessary (nesting).

## Letting pages store request-scoped data

We don't want pages storing their own data in request state. For example, a complex insert page could put mod objects in request state and use them to get form field values across the transfer of each intermediate post back. But this would make it easy for a developer working on the page to [incorrectly] use the request-scoped data for more than just restoring form field values; the developer could, for example, conditionally add a whole new section to the page. Since the request state, by definition, would not be available on the next post back, the existence of the new section of the page could change and our principle of always emulating the initial rendering of a page on the post back would be violated. The post back could also produce a "bad" concurrency error if the new section of the page included form fields.

## Cleaning out MainSequence tables

We don't clean out rows inserted into MainSequence tables because MySQL re-initializes AUTO_INCREMENT values every time it starts up by adding one to the highest value it sees in the table. If we were to delete the rows, our sequence would start over from one, and this would be very bad.

## Auto-hiding invalid links

EWF automatically hides inaccessible tabs, page actions, and other items that are part of built-in navigation areas, but doesn't automatically hide invalid links in general because hiding the link, depending on the context, could cause the page to no longer make sense. For example, imagine a text link in the middle of a paragraph. Hiding it could cause the paragraph to no longer make sense.

## Session State

On the ASP.NET platform, "session state" has typically implied a per-user, non-thread-safe data store that is automatically locked for the duration of each request that has a matching cookie. We do not support this because the automatic locking causes concurrent requests from the same user to be serialized, which sometimes has unintended consequences. Imagine a page containing 500 dynamically-served images. It's a show-stopper if the server is only able to process one at a time.

As an alternative to session state, we recommend using some type of shared data store (e.g. our AppMemoryCache class or Redis) and making it per-user and thread-safe yourself, possibly using classes from System.Collections.Concurrent. The advantage of this approach is that only resources that *actually use* the state will lock it. Another advantage is that you can avoid using a session cookie if you have access to the authenticated user from all resources that need the state.

Further reading:

*	[Brock Allen, "Think twice about using session state"](http://brockallen.com/2012/04/07/think-twice-about-using-session-state/)
*	[Good Stack Overflow question/answer](http://stackoverflow.com/q/3666556/35349)
*	[Vivek Thakur, "Cache vs Session state in ASP.NET: Replace Session with Cache"](http://codeasp.net/blogs/vivek_iit/microsoft-net/877/cache-vs-session-state-in-asp-net-replace-session-with-cache)