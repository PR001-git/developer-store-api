# DeveloperStore Sales API

A prototype Sales API for the Ambev DeveloperStore developer evaluation, built on the .NET 8 template the challenge provides. It records sales with complete CRUD, applies the quantity-based discount rules inside a DDD aggregate, requires a JWT on every sales endpoint, and writes the `SaleCreated`, `SaleModified`, `SaleCancelled` and `ItemCancelled` events to the application log.

The challenge statement is in [.doc/challenge.md](.doc/challenge.md).
