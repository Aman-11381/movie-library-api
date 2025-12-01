# 📽️ **Movie Library – A Full-Stack .NET Core + EF Core Application**

*A feature-rich backend system built to learn system design, database design, authentication, and real-world API development.*

---

## 🎯 **Project Overview**

**Movie Library** is a production-quality backend system built with **ASP.NET Core 8**, **Entity Framework Core**, and **SQL Server**.

The project simulates a real-world movie catalog system (like IMDb / TMDB), supporting:

* Full movie management (CRUD)
* Genre, Actor, Language, Country associations
* User authentication with JWT
* Secure refresh tokens using HttpOnly cookies
* User reviews & ratings (with ownership rules)
* Clean layered architecture (Controller → Service → EF Core)
* Soft real-world constraints like one-review-per-user-per-movie

This project demonstrates:

✔ Backend architecture
✔ Entity relationship modeling
✔ Clean code & DTO separation
✔ Authentication & authorization
✔ Database migrations & seeding
✔ Review/rating system
✔ Realistic movie detail responses

Perfect for showcasing **backend engineering skills** on GitHub and your resume.

---

## 🚀 **Features**

### 🔐 **Authentication & Authorization**

* JWT-based access tokens
* Refresh tokens stored in **HTTP-only secure cookies**
* Token rotation & revocation (industry best practice)
* Protected endpoints for modifying data
* Public endpoints for browsing movies

### 🎬 **Movie Management (CRUD)**

* Create, update, delete movies
* Fetch all movies
* Fetch movie by ID (with genres, actors, and reviews)

### 🏷️ **Genres**

* Many-to-many relation (Movie ↔ Genre)

### 🎭 **Actors**

* Many-to-many relation (Movie ↔ Actor)
* Store "character name" played in each movie

### 🌍 **Languages & Countries**

* Each movie has one language & one country (FK relations)

### ⭐ **Reviews & Ratings**

* Users can review a movie once (update overwrites)
* Users can only delete their own reviews
* Movie detail includes:

  * Average rating
  * Total reviews
  * Full list of reviews with usernames

### 🗃️ **Database Layer**

* SQL Server relational model
* EF Core code-first migrations
* Seed initial data on startup

---

## 🏗️ **Architecture Overview**

```
MovieLibrary/
│
├── Controllers/        → HTTP API Endpoints
├── Services/           → Business logic layer
├── Entities/           → EF Core database models
├── DTOs/               → API request/response models
├── Data/               → DbContext & Seeders
└── Program.cs          → Application bootstrap
```

This separation ensures:

* Clean API layer
* No entity exposure
* Testability
* Maintainability

---

## 🧱 **Database Schema (Simplified)**

```
Movies
│  ├── LanguageId → Languages.Id
│  ├── CountryId  → Countries.Id
│
├── MovieGenres (Many-to-many)
│     ├── MovieId → Movies.Id
│     └── GenreId → Genres.Id
│
├── MovieActors (Many-to-many)
│     ├── MovieId → Movies.Id
│     ├── ActorId → Actors.Id
│     └── CharacterName
│
├── Reviews
│     ├── MovieId → Movies.Id
│     ├── UserId  → AspNetUsers.Id
│     ├── Rating (1–5)
│     └── Comment
│
└── AspNetUsers (Identity)
```

---

## 🔐 **Authentication Flow (High-Level)**

### Login

* User logs in → Server generates:

  * **Access token (JWT)** → Returned in response
  * **Refresh token** → Stored in secure HttpOnly cookie

### Access Token Usage

* Sent with `Authorization: Bearer <token>`
* Expires after defined minutes

### Refresh Token Flow

* Sent via cookie
* Server validates → rotates → issues new access token

### Logout

* Revokes refresh token & deletes cookie

This is the same pattern used by Netflix, Google, etc.

---

## 📡 **Available Endpoints**

### 🔓 Public Endpoints

```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
GET  /api/movies
GET  /api/movies/{id}
GET  /api/movies/{id}/reviews
```

### 🔐 Protected Endpoints (Require JWT)

```
POST   /api/movies
PUT    /api/movies/{id}
DELETE /api/movies/{id}

POST   /api/movies/{id}/reviews
DELETE /api/movies/{id}/reviews/{reviewId}

POST   /api/auth/logout
```

---

## 🛠️ **Tech Stack**

| Layer                | Technology                                  |
| -------------------- | ------------------------------------------- |
| Backend Framework    | **ASP.NET Core 8 (Web API)**                |
| ORM                  | **Entity Framework Core 8**                 |
| Database             | **SQL Server / LocalDB**                    |
| Auth                 | **JWT + Refresh Tokens + ASP.NET Identity** |
| Language             | **C#**                                      |
| API Docs             | **Swagger / OpenAPI**                       |
| Dependency Injection | Built-in DI container                       |
| IDE                  | Visual Studio / VS Code                     |

---

## 🧪 **Testing the APIs**

Use Bruno/Postman/Insomnia.

### 1. Register

```
POST /api/auth/register
{
  "email": "test@example.com",
  "password": "Strong123!",
  "userName": "test"
}
```

### 2. Login

Returns JWT + sets refresh cookie.

### 3. Fetch Movies (public)

```
GET /api/movies
```

### 4. Create Movie (JWT Required)

```
POST /api/movies
Authorization: Bearer <token>
```

### 5. Post Review

```
POST /api/movies/{id}/reviews
Authorization: Bearer <token>
```

---

## 🔧 **Setup Instructions**

### 1️⃣ Clone the repository

```bash
git clone https://github.com/<your-username>/MovieLibrary.git
cd MovieLibrary
```

### 2️⃣ Update connection string

In `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=MovieLibraryDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3️⃣ Run migrations

```
Add-Migration InitialCreate
Update-Database
```

### 4️⃣ Run the project

```
dotnet run
```

Swagger opens automatically.

---

## 📈 **What I Learned / Project Highlights**

✔ Database design & normalization
✔ Many-to-many relationships in EF Core
✔ DTOs and clean separation of concerns
✔ Dependency Injection architecture
✔ Implementing JWT authentication
✔ Secure refresh token strategy with rotation
✔ Adding user-based permissions
✔ Cascading deletes & relational rules
✔ Designing real-world APIs

This project strengthened my understanding of **backend engineering**, **system design**, and **clean architecture**.

---

## 🌟 **Future Enhancements (Roadmap)**

* Movie search & advanced filtering
* Pagination & sorting
* User watchlists/favorites
* Admin panel (role-based access)
* Caching frequently accessed data
* Full frontend in React / Blazor
* Deployment (Azure App Service + Azure SQL)

---

## 👨‍💻 **About the Developer**

I am a software engineer with experience in backend development, system design, cloud technologies, and building real-world applications using the .NET ecosystem.

You can reach me on:

* LinkedIn: *(Add link here)*
* GitHub: *(Add your GitHub profile)*

---

If you want, I can also generate:

👉 A **project architecture diagram**,
👉 A **UML ER diagram**,
👉 A **flow diagram for authentication**,
👉 A **sample Postman collection**,
👉 A **badge section** for GitHub.

Would you like to add any of these to your README?
