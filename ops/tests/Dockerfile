FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY *.sln ./

# Add any new projects to the build here
COPY GE_Chatapp/GE_Chatapp/*.csproj ./GE_Chatapp/GE_Chatapp/
COPY GE_Chatapp/GE_Chatapp.Client/*.csproj ./GE_Chatapp/GE_Chatapp.Client/
COPY Chatapp.Shared/*.csproj ./Chatapp.Shared/
COPY Unit_Test/*.csproj ./Unit_Test/
COPY Integration_Test/*.csproj ./Integration_Test/
COPY RedundancyService/*.csproj ./RedundancyService/
COPY FileAPI/*.csproj ./FileAPI/
COPY SignalR/*.csproj ./SignalR/


RUN dotnet restore GE_Chatapp.sln

# Copy everything else and build
COPY . ./
RUN dotnet build GE_Chatapp.sln -c Release -o out