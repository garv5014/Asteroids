FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base 
ENV PATH="${PATH}:/root/.dotnet/tools"
# This improves the speed of the restore process
RUN dotnet tool install --global --no-cache dotnet-subset --version 0.3.2 

FROM base as restore
WORKDIR /src
COPY . .
RUN dotnet subset restore RealTimeCommunication/RealTimeCommunication.csproj --root-directory /src --output /src/restored 

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
COPY --from=restore /src/restored .
RUN dotnet restore RealTimeCommunication/RealTimeCommunication.csproj

COPY . /src
WORKDIR /src/RealTimeCommunication

ARG TARGETARCH

RUN dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime -o /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
COPY --from=restore /src/restored .
RUN dotnet restore RealTimeCommunication/RealTimeCommunication.csproj

COPY . /src
WORKDIR /src/RealTimeCommunication
CMD dotnet watch run --no-launch-profile --non-interactive

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

COPY --from=build /app .

USER $APP_UID

ENTRYPOINT ["dotnet", "RealTimeCommunication.dll"]