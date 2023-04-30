# Worlds
Contains routes and objects for interacting with NeosVR worlds, such as loading,
focusing, saving, closing or modifying them.

## Objects
### RestWorld
A subset of fields from Neos's `World` object.

| Field              | Type                                      | Description                                              |
|--------------------|-------------------------------------------|----------------------------------------------------------|
| id                 | string                                    | the session ID of the world                              |
| name               | string                                    | the name of the world                                    |
| description        | string                                    | the description of the world                             |
| access_level       | [SessionAccessLevel](#sessionaccesslevel) | the access level of the session                          |
| away_kick_interval | float                                     | the kick interval for away users (in minutes)            |
| hide_from_listing  | bool                                      | whether the world is hidden from public listing          |
| max_users          | int                                       | the maximum number of users allowed in the world (1-256) |

#### Examples
```json
{
  "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "My World",
  "description": "A place for things"
}
```

### RestUser
A subset of fields from Neos's `User` object.

| Field      | Type    | Description                              |
|------------|---------|------------------------------------------|
| id         | string  | the ID of the user                       |
| name       | string  | the name of the user                     |
| role       | string? | the role of the user                     |
| is_present | bool    | whether the user is present in the world |
| ping       | int     | the user's ping (in milliseconds)        |


#### Examples
```json
{
  "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "U-Someone",
  "role": null,
  "is_present": true,
  "ping": 10
}
```

### SessionAccessLevel
An enumeration of session access levels.

| Name             | Value | 
|------------------|-------|
| Private          | 0     |
| LAN              | 1     | 
| Friends          | 2     |
| FriendsOfFriends | 3     |
| RegisteredUsers  | 4     |
| Anyone           | 5     |

  > This type is defined by NeosVR in CloudX.Shared.

### RestUserRole
An enumeration of available roles.

| Name       | Value  |
|------------|--------|
| Admin      | 1      | 
| Builder    | 2      |
| Moderator  | 3      |
| Guest      | 4      |
| Spectator  | 5      |

  > NeosVR does not define a true enumeration for this type; the values here
  > have been picked for this mod only.

## Routes
### `GET` /worlds `200 Ok`
Gets the worlds currently loaded by the client.

#### Parameters
None.

#### Returns
An array of [RestWorld](#restworld) objects. The array may be empty if no worlds
are loaded.

#### Errors
None.

#### Examples
```json
[
  {
    "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
    "name": "My World",
    "description": "A place for things"
  },
  {
    "id": "s-a923ccdd-798f-4484-bb8f-bc9b43924cba",
    "name": "My other World",
    "description": "A place for other things"
  }
]
```

### `GET` /worlds/[{id}](#restworld) `200 Ok`
Gets a specific world currently loaded by the client.

#### Parameters
None

#### Returns
A [RestWorld](#restworld) object.

#### Errors
* `404 Not Found` if no world with the given session ID is loaded by the
  client.

#### Examples
```json
{
  "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "My World",
  "description": "A place for things"
}
```

### `POST` /worlds `201 Created`
Starts a new world.

#### Parameters
| Field    | Type   | Description                               |
|----------|--------|-------------------------------------------|
| url      | string | the Neos record URI of the world to start |
| template | string | the name of a builtin world template      |

> `url` and `template` are mutually exclusive.

#### Returns
An asynchronous [Job](job.md#job).

#### Errors
* `404 Not Found` if no known template was found with the given name
* `400 Bad Request` if neither `url` nor `template` was provided

#### Examples
```json
{
  "id": "31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "description": "start world SpaceWorld",
  "status": 0
}
```

### `POST` /worlds/[{id}](#restworld)/save `200 Ok`
Saves the world identified by `id`.

#### Parameters
None.

#### Returns
An asynchronous [Job](job.md#job).

#### Errors
* `404 Not Found` if no world with the given ID was found
* `403 Forbidden` if the world cannot be saved 

#### Examples
```json
{
  "id": "192fb88e-4581-47a1-bc2f-b2be01e65b09",
  "description": "save world SpaceWorld",
  "status": 0
}
```

### `DELETE` /worlds/[{id}](#restworld) `204 No Content`
Closes the world identified by `id`.

#### Parameters
None.

#### Returns
Nothing.

#### Errors
* `404 Not Found` if no world with the given ID was found

### `POST` /worlds/[{id}](#restworld)/restart `200 Ok`
Restarts the world identified by `id`.

#### Parameters
None.

#### Returns
An asynchronous [Job](job.md#job).

#### Errors
* `404 Not Found` if no world with the given ID was found OR it does not have
  a world handler

#### Examples
```json
{
  "id": "01a0605f-e0db-4789-9d17-6c76d57ab7ae",
  "description": "restart world SpaceWorld",
  "status": 0
}
```

### `PATCH` /worlds/[{id}](#restworld) `200 Ok`
Modifies properties of the world identified by `id`.

#### Parameters

| Field               | Type                                      | Description                                              |
|---------------------|-------------------------------------------|----------------------------------------------------------|
| ?name               | string                                    | the new name of the world                                |
| ?description        | string                                    | the new description of the world                         |
| ?access_level       | [SessionAccessLevel](#sessionaccesslevel) | the new access level of the world.                       |
| ?away_kick_interval | float                                     | the new kick interval for away users (in minutes)        |
| ?hide_from_listing  | bool                                      | whether the world is hidden from public listing          |
| ?max_users          | int                                       | the maximum number of users allowed in the world (1-256) |

  > While all parameters to this route are optional, at least one must be set.

#### Returns
The updated [RestWorld](#restworld).

#### Errors
* `404 Not Found` if no world with the given ID was found
* `400 Bad Request` if 
  1. no parameters are provided OR
  2. `access_level` is not a recognized value OR
  3. `hide_from_listing` is not a boolean OR
  4. `away_kick_interval` is not a number greater than or equal to zero OR
  5. `max_users` is not within 1 and 256

#### Examples
```json
{
  "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "My New World Name",
  "description": "A place for things"
}
```

### `GET` /worlds/[{id}](#restworld)/users `200 Ok`
Gets the users currently in the world identified by `id`.

#### Parameters
None.

#### Returns
An array of [RestUser](#restuser) objects.

#### Errors
* `404 Not Found` if no world with the given ID was found

#### Examples
```json
[
  {
    "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
    "name": "U-Someone",
    "role": null,
    "is_present": true,
    "ping": 10
  }
]
```

### `GET` /worlds/[{id}](#restworld)/users/[{id}](#restuser) `200 Ok`
Gets a specific user currently in the world identified by `id`.

#### Parameters
None.

#### Returns
A [RestUser](#restuser) object.

#### Errors
* `404 Not Found` if 
  1. no world with the given ID was found OR
  2. no user with the given ID was found

#### Examples
```json
{
  "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "U-Someone",
  "role": null,
  "is_present": true,
  "ping": 10
}
```

### `POST` /worlds/[{id}](#restworld)/users/[{id}](#restuser)/kick `204 No Content`
Kicks the given user from the given world.

#### Parameters
None.

#### Returns
Nothing.

#### Errors
* `404 Not Found` if
  1. no world with the given ID was found OR
  2. no user with the given ID was found
* `401 Forbidden` if
  1. the kicked user is the host OR
  2. the current user doesn't have permission to kick

### `POST` /worlds/[{id}](#restworld)/users/[{id}](#restuser)/ban `204 No Content`
Bans the given user from all sessions hosted by this server.

#### Parameters
None.

#### Returns
Nothing.

#### Errors
* `404 Not Found` if
  1. no world with the given ID was found OR
  2. no user with the given ID was found
* `401 Forbidden` if
  1. the banned user is the host OR
  2. the current user doesn't have permission to ban

### `POST` /worlds/[{id}](#restworld)/users/[{id}](#restuser)/silence `204 No Content`
Silences or unsilences the given user in the given world.

#### Parameters
| Field    | Type | Description                         |
|----------|------|-------------------------------------|
| silenced | bool | whether the user should be silenced |

#### Returns
Nothing.

#### Errors
* `404 Not Found` if
  1. no world with the given ID was found OR
  2. no user with the given ID was found
* `400 Bad Request` if 
  1. `silenced` is not present OR
  2. `silenced` is not a boolean

### `POST` /worlds/[{id}](#restworld)/users/[{id}](#restuser)/respawn `204 No Content`
Respawns the given user in the given world.

#### Parameters
None.

#### Returns
Nothing.

#### Errors
* `404 Not Found` if
  1. no world with the given ID was found OR
  2. no user with the given ID was found

### `PUT` /worlds/[{id}](#restworld)/users/[{id}](#restuser)/role `204 No Content`
Sets the role of the given user in the given world.

#### Parameters
| Field | Type                          | Description        |
|-------|-------------------------------|--------------------|
| role  | [RestUserRole](#restuserrole) | the role to assign |

#### Returns
Nothing.

#### Errors
* `404 Not Found` if
  1. no world with the given ID was found OR
  2. no user with the given ID was found
* `400 Bad Request` if 
  1. `role` is not set OR
  2. `role` is not a recognized value
* `403 Forbidden` if the role is higher than the host's role

### `GET` /worlds/focused `200 Ok`
Gets the currently focused world.

#### Parameters
None

#### Returns
A [RestWorld](#restworld) object.

#### Errors
* `404 Not Found` if no world is currently focused

#### Examples
```json
{
  "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "My World",
  "description": "A place for things"
}
```

### `PUT` /worlds/focused `200 Ok`
Sets the currently focused world.

#### Parameters
| Field  | Type   | Description                    |
|--------|--------|--------------------------------|
| ?id    | string | the id of the world to focus   |
| ?name  | string | the name of the world to focus |
| ?index | int    | the loaded index of the world  |

  > At least one of `id`, `name`, or `index` must be set.

#### Returns
A [RestWorld](#restworld) object.

#### Errors
* `404 Not Found` if no world is currently focused

#### Examples
```json
{
  "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
  "name": "My World",
  "description": "A place for things"
}
```

### `POST` /worlds/[{id}](#restworld)/impulses/{tag} `204 No Content`
Sends a dynamic impulse to the given world. The path component `tag` identifies
the impulse tag to trigger.

#### Parameters
| Field   | Type               | Description                                |
|---------|--------------------|--------------------------------------------|
| ?value  | float, int, string | the value to send to the impulse receivers |

  > `value` can be omitted entirely or be either a float, an int, or a string.
  > Different impulse receivers will fire depending on the type of the value.

#### Returns
Nothing.

#### Errors
* `404 Not Found` if no world with the given ID was found 
