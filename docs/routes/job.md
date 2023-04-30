# Jobs
## Objects
### Job
An asynchronous job descriptor.

| Field       | Type                    | Description                             |
|-------------|-------------------------|-----------------------------------------|
| id          | guid                    | the ID of the job                       |
| description | string                  | a human-readable description of the job |
| status      | [JobStatus](#JobStatus) | the current status of the job           |

#### Examples
```json
{
     "id": "31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
     "description":" start world SpaceWorld",
     "status": 0
}
```

### JobStatus
An enumeration of valid values for the `status` field of the [Job](#Job) object.

| Name      | Value |
|-----------|-------|
| Running   | 1     |
| Completed | 2     | 
| Canceled  | 3     |
| Faulted   | 4     |


## Routes
### `GET` /jobs/[{id}](#Job)
Gets information about a specific job currently being executed by the client.

> Note that retrieving a completed job counts as taking receipt of its
> completion and will result in its removal from the internal job tracker.

#### Parameters
None

#### Returns
A [Job](#Job) object.

#### Errors
* `400 Bad Request` if the ID cannot be parsed as a GUID
* `404 Not Found` if no job with the given ID exists

#### Examples
```json
{
    "id": "s-31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
    "name": "My World",
    "description": "A place for things"
}
```
