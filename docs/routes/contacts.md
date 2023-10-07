# Contacts
Contains routes and objects for interacting with Resonite contacts, such as 
accepting friend requests or checking block lists.

## Objects
### RestContact
A subset of fields from Resonite's `Contact` object.

| Field         | Type                                    | Description                                  |
|---------------|-----------------------------------------|----------------------------------------------|
| id            | string                                  | the user's id                                |
| username      | string                                  | the user's username                          |
| friend_status | [RestContactStatus](#restcontactstatus) | the status of the friend request             |
| is_accepted   | bool                                    | whether the friend request has been accepted |

#### Examples
```json
{
    "id": "u-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
    "username": "U-Someone",
    "friend_status": 2,
    "is_accepted": false
}
```

### RestContactStatus
An enumeration of contact request states.

| Name      | Value |
|-----------|-------|
| None      | 0     | 
| Ignored   | 1     |
| Blocked   | 2     |
| Friend    | 3     |
| Requested | 4     |

## Routes
### `GET` /contacts `200 Ok`
Gets all known contacts.

#### Parameters
None.

#### Returns
An array of [RestContact](#restcontact) objects. The array may be empty if the
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

### `PATCH` /contacts/[{id}](#restcontact) `204 No Content`
Modifies the status of the given contact. This can be used to remove contacts, 
accept friend requests, etc.

  > The user id may be substituted for the user's username on this route.

#### Parameters
| Field  | Type                                    | Description                   |
|--------|-----------------------------------------|-------------------------------|
| status | [RestContactStatus](#restcontactstatus) | the new status of the request |

#### Returns
Nothing.

#### Errors
* `400 Bad Request` if 
  1. `status` is not set OR
  2. `status` is not a recognized value OR
  3. `status` is `Requested` or `SearchResult`
* `404 Not Found` if no matching friend or friend request could be found
