# Worlds
Contains routes and objects for interacting with NeosVR friends, such as 
accepting friend requests or checking block lists.

## Objects
### RestFriend
A subset of fields from Neos's `Friend` object.

| Field         | Type                          | Description                                  |
|---------------|-------------------------------|----------------------------------------------|
| id            | string                        | the user's id                                |
| username      | string                        | the user's username                          |
| friend_status | [FriendStatus](#friendstatus) | the status of the friend request             |
| is_accepted   | bool                          | whether the friend request has been accepted |

#### Examples
```json
{
    "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
    "username": "U-Someone",
    "friend_status": 2,
    "is_accepted": false
}
```

### FriendStatus
An enumeration of friend request states.

| Name         | Value |
|--------------|-------|
| None         | 0     |
| SearchResult | 1     |
| Requested    | 2     | 
| Ignored      | 3     |
| Blocked      | 4     |
| Accepted     | 5     |

  > This type is defined by NeosVR in CloudX.Shared.

## Routes
### `GET` /friends `200 Ok`
Gets all accepted friend requests.

#### Parameters
None.

#### Returns
An array of [RestFriend](#restfriend) objects. The array may be empty if the
account has no friends.

#### Errors
None.

#### Examples
```json
[
    {
        "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
        "username": "U-Someone",
        "friend_status": 5,
        "is_accepted": false
    },
    {
        "id": "u-3f96165c-3f40-4acf-87b6-298a23258157",
        "username": "U-SomeoneElse",
        "friend_status": 5,
        "is_accepted": false
    }
]
```

### `PATCH` /friends/[{id}](#restfriend) `204 No Content`
Modifies the status of the given friend request.

  > The user id may be substituted for the user's username on this route.

#### Parameters
| Field  | Type                          | Description                   |
|--------|-------------------------------|-------------------------------|
| status | [FriendStatus](#friendstatus) | the new status of the request |

#### Returns
Nothing.

#### Errors
* `400 Bad Request` if 
  1. `status` is not set OR
  2. `status` is not a recognized value OR
  3. `status` is `Requested` or `SearchResult`
* `404 Not Found` if no matching friend or friend request could be found
