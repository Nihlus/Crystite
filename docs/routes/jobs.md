# Jobs
Contains routes and objects for querying and interacting with asynchronous jobs
started by other routes.

## Objects
### Job
An asynchronous job descriptor.

| Field       | Type                    | Description                             |
|-------------|-------------------------|-----------------------------------------|
| id          | guid                    | the ID of the job                       |
| description | string                  | a human-readable description of the job |
| status      | [JobStatus](#jobstatus) | the current status of the job           |

#### Examples
```json
{
     "id": "31094fc0-e8ad-4398-bbf3-14d0310a6e9d",
     "description":" start world SpaceWorld",
     "status": 0
}
```

### JobStatus
An enumeration of valid values for the `status` field of the [Job](#job) object.

| Name      | Value |
|-----------|-------|
| Running   | 1     |
| Completed | 2     | 
| Canceled  | 3     |
| Faulted   | 4     |


## Routes
### `GET` /jobs/[{id}](#job)
Gets information about a specific job currently being executed by the client.

> Note that retrieving a completed job counts as taking receipt of its
> completion and will result in its removal from the internal job tracker.

#### Parameters
None

#### Returns
A [Job](#job) object.

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

### `DELETE` /jobs/[{id}](#job)
Cancels a specific job currently being executed by the client.

> Cancelling a job is not an instant operation; it is merely a request for the
> ongoing task. As such, requesting a cancellation may not stop the job from
> completing.
> 
> As with the `GET` rote, receiving a completed job from this route counts as
> taking receipt of its completion.

#### Parameters
None

#### Returns
A [Job](#job) object.

#### Errors
* `400 Bad Request` if the ID cannot be parsed as a GUID
* `404 Not Found` if no job with the given ID exists
* 
