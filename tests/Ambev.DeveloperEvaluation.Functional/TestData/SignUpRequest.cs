namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>POST /api/users</c>, with the status and role written as strings, the way a client sends them.
/// </summary>
/// <param name="Username">The username, which the API returns as <c>name</c>.</param>
/// <param name="Password">The password in plain text.</param>
/// <param name="Phone">The phone number.</param>
/// <param name="Email">The email address, which must not be signed up yet.</param>
/// <param name="Status">The account status, such as <c>Active</c> or <c>Inactive</c>.</param>
/// <param name="Role">The role. Public sign-up only accepts <c>Customer</c>; anything else is a validation error.</param>
public sealed record SignUpRequest(string Username, string Password, string Phone, string Email, string Status, string Role);
