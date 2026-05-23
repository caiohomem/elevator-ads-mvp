# Architecture

Elevator Ads MVP is intended to use a layered backend architecture and a standalone frontend application.

## Backend

The backend is organized as a .NET solution with the following project responsibilities:

- `ElevatorAds.Api`: HTTP API layer and application composition root.
- `ElevatorAds.Application`: future use cases and application services.
- `ElevatorAds.Domain`: future domain entities, value objects, and business rules.
- `ElevatorAds.Infrastructure`: future persistence and external integrations.
- `ElevatorAds.Tests`: automated tests for the backend.

The intended dependency direction is:

```text
Api -> Application -> Domain
Infrastructure -> Application
Tests -> Api / Application / Domain
```

PostgreSQL and Entity Framework Core will be added later in `Infrastructure`. This bootstrap phase does not include database integration, authentication, or business entities.

## DOOH Delivery Model

The MVP delivery model is scheduled playlist delivery, not real-time ad serving. In the elevator and Otis-style environment, the screen or player downloads a schedule once per day and executes that same programmed sequence repeatedly throughout the day without requesting an ad before each playback.

### DailyPlaylist

`DailyPlaylist` is the schedule document for a specific screen on a specific day. It represents the ordered sequence the player should run locally after downloading the schedule.

### DailyPlaylistItem

`DailyPlaylistItem` is a single entry in the ordered schedule. It identifies which creative should play, for how long, and where it appears in the playlist sequence.

### Playlist generation

Playlist generation is the process that evaluates active campaigns, approved creatives, and campaign delivery constraints to produce a daily ordered schedule for each screen. For the MVP, this is the first delivery mechanism to build before any real-time decisioning.

### Playlist publishing

Once generated, the playlist is published as a downloadable artifact for the screen or player. Persistence can begin with simple application storage and later move to PostgreSQL when database support is introduced.

### Playlist versioning

Playlists should support versioning so a screen can detect when a newer schedule has been published for the same day. This allows operational updates, such as removing cancelled campaign content, without introducing per-impression decisioning.

### Playlist download by screen/player

The screen or player downloads its playlist from a single schedule endpoint, typically once per day or on a low-frequency poll. The MVP does not require per-playback or per-impression server requests.

### Same sequence repeated throughout the day

After downloading the playlist, the player loops through the same ordered sequence locally for the rest of the day. Repeated playback is controlled on the player side, not by requesting the next ad from the backend in real time.

### Playback and proof-of-play reporting

Playback reporting happens after execution. The player can later submit proof-of-play or event batches describing what ran, keeping reporting concerns separate from playlist delivery.

### Out of scope for the MVP

The MVP does not implement real-time `next-ad` serving, auction engine behavior, DSP/SSP bidding, or OpenRTB integration. Those concepts come later, after scheduled playlist delivery and reporting are in place.

## Frontend

The frontend is a Next.js TypeScript app under `frontend/elevator-ads-admin`.

For this bootstrap phase, the frontend is a basic admin shell only. It does not call the backend and does not include dashboard features.
