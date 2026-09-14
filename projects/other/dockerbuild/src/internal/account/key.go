// Package account 负责网关 Key 鉴权管理。
// Key 持久化到 key.json（与 config.toml 同目录），调用网关 /v1/* 必须携带该 Key。
package account

import (
	"crypto/rand"
	"crypto/subtle"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sync"
)

// keyFile 定义 key.json 的磁盘格式。
type keyFile struct {
	Key string `json:"key"`
}

// KeyStore 管理网关 Key。
// Key 持久化到磁盘，支持自定义设置与随机生成。
type KeyStore struct {
	path string
	mu   sync.RWMutex
	key  string
}

// NewKeyStore 创建 Key 存储。
// 若 key.json 不存在则自动生成随机 Key（32 位十六进制）并写入；存在则读取。
func NewKeyStore(path string) (*KeyStore, error) {
	s := &KeyStore{path: path}

	data, err := os.ReadFile(path)
	if err != nil {
		if !errors.Is(err, os.ErrNotExist) {
			return nil, fmt.Errorf("account: read key file: %w", err)
		}
		// 文件不存在，生成随机 Key 并写入
		key, err := generateRandomKey()
		if err != nil {
			return nil, err
		}
		s.key = key
		if err := s.save(); err != nil {
			return nil, err
		}
		return s, nil
	}

	var kf keyFile
	if err := json.Unmarshal(data, &kf); err != nil {
		return nil, fmt.Errorf("account: decode key file: %w", err)
	}
	if kf.Key == "" {
		return nil, errors.New("account: key.json 中 key 为空")
	}
	s.key = kf.Key
	return s, nil
}

// GetKey 返回当前 Key。
func (s *KeyStore) GetKey() string {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.key
}

// SetKey 设置自定义 Key（校验非空），并原子写回 key.json。
func (s *KeyStore) SetKey(key string) error {
	if key == "" {
		return errors.New("account: key 不能为空")
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	s.key = key
	return s.save()
}

// GenerateKey 随机生成新 Key（32 位十六进制），写回并返回。
func (s *KeyStore) GenerateKey() (string, error) {
	key, err := generateRandomKey()
	if err != nil {
		return "", err
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	s.key = key
	if err := s.save(); err != nil {
		return "", err
	}
	return key, nil
}

// Validate 校验传入 Key 是否匹配。
// 使用 subtle.ConstantTimeCompare 进行常量时间比较，防止时序攻击。
func (s *KeyStore) Validate(key string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return subtle.ConstantTimeCompare([]byte(s.key), []byte(key)) == 1
}

// generateRandomKey 生成 32 位十六进制随机 Key（16 字节，crypto/rand）。
func generateRandomKey() (string, error) {
	buf := make([]byte, 16)
	if _, err := rand.Read(buf); err != nil {
		return "", fmt.Errorf("account: generate random key: %w", err)
	}
	return hex.EncodeToString(buf), nil
}

// save 把当前 Key 原子写回 key.json（0600 权限）。
// 先写临时文件再 rename，避免写一半导致文件损坏。
// 调用方需持有写锁。
func (s *KeyStore) save() error {
	data, err := json.Marshal(keyFile{Key: s.key})
	if err != nil {
		return fmt.Errorf("account: marshal key: %w", err)
	}

	dir := filepath.Dir(s.path)
	tmp, err := os.CreateTemp(dir, "key-*.tmp")
	if err != nil {
		return fmt.Errorf("account: create temp file: %w", err)
	}
	tmpName := tmp.Name()
	defer os.Remove(tmpName) // rename 成功后删除是空操作

	if err := tmp.Chmod(0o600); err != nil {
		_ = tmp.Close()
		return fmt.Errorf("account: chmod temp file: %w", err)
	}
	if _, err := tmp.Write(data); err != nil {
		_ = tmp.Close()
		return fmt.Errorf("account: write temp file: %w", err)
	}
	if err := tmp.Close(); err != nil {
		return fmt.Errorf("account: close temp file: %w", err)
	}
	if err := os.Rename(tmpName, s.path); err != nil {
		return fmt.Errorf("account: rename temp file: %w", err)
	}
	return nil
}
