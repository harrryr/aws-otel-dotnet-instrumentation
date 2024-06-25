#!/bin/bash
# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: Apache-2.0

# Fail fast
set -e

# Check script is running in contract-tests
current_path=`pwd`
current_dir="${current_path##*/}"
if [ "$current_dir" != "test" ]; then
  echo "Please run from test dir"
  exit
fi

# Remove old whl files (excluding distro whl)
rm -rf dist/mock_collector*
rm -rf dist/contract_tests*

# Install python dependency for contract-test
pip3 install pymysql
pip3 install cryptography

# To be clear, install binary for psycopg2 have no negative influence on otel here
# since Otel-Instrumentation running in container that install psycopg2 from source
pip3 install sqlalchemy psycopg2-binary

# Create mock-collector image
cd contract-tests/images/mock-collector
docker build . -t aws-application-signals-mock-collector
if [ $? = 1 ]; then
  echo "Docker build for mock collector failed"
  exit 1
fi

# Currently we do not have sample applications for contract tests.
# Following script will need to be updated once we have sample applications ready
# Tracking: https://github.com/aws-observability/aws-otel-dotnet-instrumentation/issues/37

# Find and store aws_opentelemetry_distro whl file
# cd ../../../dist
# DISTRO=(aws_opentelemetry_distro-*-py3-none-any.whl)
# if [ "$DISTRO" = "aws_opentelemetry_distro-*-py3-none-any.whl" ]; then
#  echo "Could not find aws_opentelemetry_distro whl file in dist dir."
#  exit 1
# fi

# Create application images
# cd ..
# for dir in contract-tests/images/applications/*
# do
#   application="${dir##*/}"
#   docker build . -t aws-application-signals-tests-${application}-app -f ${dir}/Dockerfile --build-arg="DISTRO=${DISTRO}"
#   if [ $? = 1 ]; then
#     echo "Docker build for ${application} application failed"
#     exit 1
#   fi
# done

# Build and install mock-collector
# cd contract-tests/images/mock-collector
python3 -m build --outdir ../../../dist
cd ../../../dist
pip3 install mock_collector-1.0.0-py3-none-any.whl --force-reinstall

# Build and install contract-tests
cd ../contract-tests/tests
python3 -m build --outdir ../../dist
cd ../../dist
# --force-reinstall causes `ERROR: No matching distribution found for mock-collector==1.0.0`, but uninstalling and reinstalling works pretty reliably.
pip3 uninstall contract-tests -y
pip3 install contract_tests-1.0.0-py3-none-any.whl