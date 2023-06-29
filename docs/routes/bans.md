# Bans
Contains routes and objects for handling server-wide bans of users.

## Objects
### RestBan
| Field      | Type    | Description                         |
|------------|---------|-------------------------------------|
| id         | string  | the id of the banned user           |
| username   | string  | the username of the banned user     |
| machine_id | string? | the id of the banned user's machine |

#### Examples
```json
{
    "id": "u-6990349b-911f-4ff3-bc4f-9b6f7bb20cb4",
    "username": "U-Bastard",
    "machine_id": "m-b13b4f92-b240-4e85-8151-4a455f9901e9"
}
```

## Routes
### `GET` /bans `200 OK`
Gets the active bans.

#### Parameters
None.

#### Returns
An array of [RestBan](#restban) objects. The array may be empty if no users are
banned.

#### Errors
None.

#### Examples
```json
[
    {
        "id": "u-6990349b-911f-4ff3-bc4f-9b6f7bb20cb4",
        "username": "U-Bastard",
        "machine_id": "m-b13b4f92-b240-4e85-8151-4a455f9901e9"
    }
]
```

### `POST` /bans/[{id}](worlds.md#restuser) `200 Ok`
Bans the identified user from all sessions on this server.

  > The user id may be substituted for the user's username on this route.

#### Parameters
None.

#### Returns
A [RestBan](#restban) object.

#### Errors
* `404 Not Found` if no matching user could be found

#### Examples
```json
{
    "id": "u-6990349b-911f-4ff3-bc4f-9b6f7bb20cb4",
    "username": "U-Bastard",
    "machine_id": "m-b13b4f92-b240-4e85-8151-4a455f9901e9"
}
```

### `DELETE` /bans/[{id}](worlds.md#restuser) `201 No Content`
Unbans the identified user from all sessions on this server.

  > The user id may be substituted for the user's username on this route.

#### Parameters
None.

#### Returns
Nothing

#### Errors
* `404 Not Found` if no matching user could be found
