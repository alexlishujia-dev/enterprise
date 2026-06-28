<template>
  <u-tabbar
    :value="active"
    :fixed="true"
    :placeholder="true"
    :safe-area-inset-bottom="true"
    active-color="#1677ff"
    inactive-color="#8c8c8c"
    @change="onChange">
    <u-tabbar-item name="index" text="工作台" icon="home" />
    <u-tabbar-item name="users" text="用户" icon="account" />
    <u-tabbar-item name="mine" text="我的" icon="account-fill" />
  </u-tabbar>
</template>

<script setup>
const props = defineProps({
  active: { type: String, default: 'index' }
});

const routes = {
  index: '/pages/index/index',
  users: '/pages/system/user-list/user-list',
  mine: '/pages/mine/mine'
};

function onChange(name) {
  if (name === props.active) return;
  const url = routes[name];
  if (url) uni.reLaunch({ url });
}
</script>
