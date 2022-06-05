# use an official microsoft SDK as a parent image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# set our working directory to /app
WORKDIR /app

# copy the working directory contents into the container at /app
COPY . .
# copy a dummy appsettings.json file in, since this file is secret
COPY config/appsettings-sample.json /app/config/appsettings.json

# build and put files in a folder called output
RUN dotnet build -c Release -o output

# Build runtime image. Note use of runtime version of core image to reduce size of final image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime-env
WORKDIR /app
COPY --from=build-env app/output .

# create an empty directory for downloaded data
RUN mkdir /app/Data

# Once the container launches, run the console
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "dotnet-roslyn-dynamic-api.dll"]