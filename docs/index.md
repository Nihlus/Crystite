# General Information
All objects received from the API are JSON-serialized. Request parameters
are either 
  1. passed as path components when they identify a resource, or
  2. passed as url-encoded form fields when they carry data

Path components that should be replaced by a value are enclosed in curly braces
in the route specification. The component links to the object that defines the
field from which the value should be taken, which is in turn specified by the 
text within the curly braces.

Types and fields may be marked optional or nullable in the documentation. An
optional field is indicated by a leading question mark on its (`?field`)
and a nullable field with a trailing question mark on its type `string?`.

Enumerations are serialized as integers unless otherwise specified.

# Resource Routes
1. [Bans](routes/bans.md)
2. [Contacts](routes/contacts.md)
3. [Jobs](routes/jobs.md)
4. [Worlds](routes/worlds.md)
