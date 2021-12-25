#!/bin/bash
set -x
awslocal s3 mb s3://test
awslocal s3api put-bucket-acl --bucket test --acl public-read
set +x