# .Net productsmanagement Api
##


here is a detailed guide on my productsManagement project 

# 1. Installation
##
###  1.clone the repository with the following  command and then install the dependencies

```
 git clone 
```

### 2. install SDK .Net 10 into your machine 

```
winget install Microsoft.DotNet.SDK.10
```
or download from the official Microsoft website via the [Microsoft .NET Download page](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

### 3. use the following commnd to download all the independencies and nudget packages  stated in the project 
```
dotnet restore

```
### 4. use the following command for compilation and errors that may arise
```
dotnet build

````

### 5. use the following to start and run the project

```
dotnet run
````

# 2. Configuration
##

### Create a `config.yaml` file in your root directory with the following structure which with the details:

```
services:                 # required — one entry per container
  <service-name>:
    image: ...             #   OR build a custom image:
    build:
      context: .            # folder sent to the Docker build
      dockerfile: path/to/Dockerfile
    environment:            # env vars injected into the container
      - KEY=value
    ports:
      - "hostPort:containerPort"
    volumes:
      - name-or-path:/container/path
    depends_on:
      other-service:
        condition: service_healthy   # wait for its healthcheck to pass
    healthcheck:            # how Compose checks if the container is "ready"
      test: ["CMD", "..."]
      interval: 10s
      timeout: 5s
      retries: 10

volumes:                  # named volumes referenced above, declared once
  name-or-path:

networks:                 # optional — Compose creates a default one for you
  ...

  ```

 #### sample app.settings.json for your project

  ```
  Connection string: Server=ms-sql-server,1433;Database=YourDb;User Id=sa;Password=Pa5S5w0rd2021;TrustServerCertificate=True;Encrypt=False

  ```



#### you can either use a command to start dokcer or choose to open the desktop dokcer application and wait until you see `engine running`

#### for more info about docker you can check this [repository](https://github.com/Njuguna-Peter-njoroge/productsManangement/blob/master/docker.md)
